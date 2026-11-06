// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Companion's WebSocket RTSP/RTP gateway over a single outbound TCP connection.
/// Owns no native worker or Unity synchronization-context continuation.
/// Stop closes the socket to interrupt reads; it never joins a worker on Unity's main thread.
/// Callbacks execute on a pool thread and must not call Unity APIs.
/// </summary>
public sealed class NeonTcpGazeClient : IDisposable
{
    private readonly string ip;
    private readonly int port;
    private readonly Action<byte[], long, bool> onGaze;
    private readonly Action<string> onStatus;
    private readonly object gate = new object();
    private ClientWebSocket socket;
    private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
    private bool stopped;
    private string session;
    private int sequence;
    private int rtpChannel, rtcpChannel = 1;
    private int pendingChannel = -1, pendingLength;
    private MemoryStream pendingPacket;
    private readonly NeonRtpClock clock = new NeonRtpClock();
    private readonly byte[] receiveBuffer = new byte[8192];
    private long lastGazeTicks;

    public NeonTcpGazeClient(string ip, int port, Action<byte[], long, bool> onGaze, Action<string> onStatus)
    {
        this.ip = ip; this.port = port; this.onGaze = onGaze; this.onStatus = onStatus;
    }

    public Task RunAsync() => Task.Run(Run);

    private async Task Run()
    {
        ClientWebSocket connection;
        lock (gate)
        {
            if (stopped) return;
            connection = socket = new ClientWebSocket();
        }
        try
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token))
            {
                timeout.CancelAfter(5000);
                await connection.ConnectAsync(new Uri("ws://" + ip + ":" + port), timeout.Token).ConfigureAwait(false);
            }
            {
                string url = "rtsp://" + ip + ":" + port + "/";
                var description = await Request(connection, "DESCRIBE", url + "?camera=gaze", "Accept: application/sdp\r\n").ConfigureAwait(false);
                if (!description.Body.Contains("com.pupillabs.gaze")) throw new IOException("gaze_stream_not_advertised");
                // Current Companion advertises a 90 kHz gaze RTP clock.
                if (description.Body.IndexOf("/90000", StringComparison.Ordinal) < 0)
                    throw new IOException("unsupported_gaze_clock_rate");
                var setup = await Request(connection, "SETUP", url, "Transport: RTP/AVP/TCP;unicast;interleaved=0-1\r\n").ConfigureAwait(false);
                if (!setup.Headers.TryGetValue("Session", out session) || string.IsNullOrWhiteSpace(session))
                    throw new IOException("missing_rtsp_session");
                session = session.Split(';')[0].Trim();
                string transport;
                if (!setup.Headers.TryGetValue("Transport", out transport)) throw new IOException("missing_transport");
                // The Companion WS gateway can report its internal UDP leg here.
                // The PC still receives binary messages on the outbound WebSocket, never inbound UDP.
                var channels = System.Text.RegularExpressions.Regex.Match(transport, @"interleaved=(\d+)-(\d+)");
                if (channels.Success)
                {
                    rtpChannel = int.Parse(channels.Groups[1].Value, CultureInfo.InvariantCulture);
                    rtcpChannel = int.Parse(channels.Groups[2].Value, CultureInfo.InvariantCulture);
                }
                await Request(connection, "PLAY", url, "Session: " + session + "\r\nRange: npt=0-\r\n").ConfigureAwait(false);
                onStatus?.Invoke("websocket_playing_waiting_for_gaze");
                lastGazeTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                long lastKeepAlive = lastGazeTicks;
                while (!IsStopped)
                {
                    await ReadMessage(connection).ConfigureAwait(false);
                    long now = System.Diagnostics.Stopwatch.GetTimestamp();
                    if ((now - lastGazeTicks) / (double)System.Diagnostics.Stopwatch.Frequency > 10)
                        throw new IOException("tcp_no_gaze_for_10_seconds");
                    if ((now - lastKeepAlive) / (double)System.Diagnostics.Stopwatch.Frequency > 15)
                    {
                        await Send(connection, "OPTIONS", url, "Session: " + session + "\r\n").ConfigureAwait(false);
                        lastKeepAlive = now;
                    }
                }
            }
        }
        catch (Exception) when (IsStopped) { }
        finally
        {
            connection.Dispose();
            pendingPacket?.Dispose();
            lock (gate) { if (ReferenceEquals(socket, connection)) socket = null; }
        }
    }

    private bool IsStopped { get { lock (gate) return stopped; } }

    private async Task<Response> Request(ClientWebSocket stream, string method, string url, string headers)
    {
        int expected = await Send(stream, method, url, headers).ConfigureAwait(false);
        while (!IsStopped)
        {
            var response = await ReadMessage(stream).ConfigureAwait(false);
            if (response == null) continue;
            string value;
            if (!response.Headers.TryGetValue("CSeq", out value) || value != expected.ToString(CultureInfo.InvariantCulture))
                continue;
            if (!response.FirstLine.StartsWith("RTSP/1.0 200 ", StringComparison.Ordinal))
                throw new IOException(method + "_rejected_" + response.FirstLine);
            return response;
        }
        throw new OperationCanceledException();
    }

    private async Task<int> Send(ClientWebSocket stream, string method, string url, string headers)
    {
        int seq = ++sequence;
        byte[] bytes = Encoding.ASCII.GetBytes(method + " " + url + " RTSP/1.0\r\nCSeq: " + seq + "\r\n" + headers + "\r\n");
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token))
        {
            timeout.CancelAfter(2000);
            await stream.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, timeout.Token).ConfigureAwait(false);
        }
        return seq;
    }

    private sealed class Response
    {
        public string FirstLine, Body;
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Response> ReadMessage(ClientWebSocket stream)
    {
        byte[] message;
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token))
        using (var buffer = new MemoryStream())
        {
            timeout.CancelAfter(8000);
            byte[] chunk = receiveBuffer; WebSocketReceiveResult result;
            do
            {
                result = await stream.ReceiveAsync(new ArraySegment<byte>(chunk), timeout.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) throw new EndOfStreamException("websocket_closed");
                if (buffer.Length + result.Count > 65536) throw new IOException("websocket_message_too_large");
                buffer.Write(chunk, 0, result.Count);
            } while (!result.EndOfMessage);
            message = buffer.ToArray();
        }
        if (message.Length == 0) return null;
        if (pendingChannel >= 0)
        {
            AppendInterleaved(message, 0, message.Length);
            return null;
        }
        if (message[0] == '$')
        {
            if (message.Length < 4) throw new IOException("invalid_interleaved_header");
            pendingChannel = message[1]; pendingLength = (message[2] << 8) | message[3];
            pendingPacket = new MemoryStream();
            AppendInterleaved(message, 4, message.Length - 4);
            return null;
        }
        // Companion's gateway usually sends bare RTP/RTCP in each WebSocket message.
        if (message.Length >= 2 && message[0] >> 6 == 2)
        {
            if (message[1] >= 192 && message[1] <= 223) clock.ReadSenderReports(message);
            else ReceiveRtp(message);
            return null;
        }
        string text = Encoding.UTF8.GetString(message);
        int headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd < 0 || headerEnd > 16384) throw new IOException("invalid_rtsp_header");
        string[] lines = text.Substring(0, headerEnd).Split(new[] { "\r\n" }, StringSplitOptions.None);
        var response = new Response { FirstLine = lines[0] };
        for (int i = 1; i < lines.Length; i++)
        {
            int colon = lines[i].IndexOf(':');
            if (colon > 0) response.Headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
        }
        string lengthText; int length = 0;
        if (response.Headers.TryGetValue("Content-Length", out lengthText) &&
            (!int.TryParse(lengthText, out length) || length < 0 || length > 65536)) throw new IOException("invalid_rtsp_body_length");
        if (headerEnd + 4 + length > message.Length) throw new IOException("truncated_rtsp_body");
        response.Body = Encoding.UTF8.GetString(message, headerEnd + 4, length);
        return response;
    }

    private void AppendInterleaved(byte[] bytes, int offset, int length)
    {
        // Companion may send the '$' header and the packet as separate WS messages.
        if (pendingPacket.Length + length > pendingLength) throw new IOException("interleaved_packet_length_mismatch");
        pendingPacket.Write(bytes, offset, length);
        if (pendingPacket.Length < pendingLength) return;
        byte[] packet = pendingPacket.ToArray(); int channel = pendingChannel;
        pendingPacket.Dispose(); pendingPacket = null; pendingChannel = -1;
        if (channel == rtcpChannel) clock.ReadSenderReports(packet);
        else if (channel == rtpChannel) ReceiveRtp(packet);
    }

    private void ReceiveRtp(byte[] packet)
    {
        int offset, length;
        if (!TryGetGazePayload(packet, out offset, out length)) return;
        long timestamp;
        bool synchronized = clock.TryMap(packet, out timestamp);
        var payload = new byte[length]; Buffer.BlockCopy(packet, offset, payload, 0, length);
        lastGazeTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        onGaze?.Invoke(payload, timestamp, synchronized);
    }

    public static bool TryGetGazePayload(byte[] packet, out int offset, out int length)
    {
        offset = length = 0;
        if (packet == null || packet.Length < 12 || packet[0] >> 6 != 2 || (packet[1] & 127) != 99) return false;
        offset = 12 + (packet[0] & 15) * 4;
        if (offset > packet.Length) return false;
        if ((packet[0] & 16) != 0)
        {
            if (offset + 4 > packet.Length) return false;
            int extensionWords = (packet[offset + 2] << 8) | packet[offset + 3];
            offset += 4 + extensionWords * 4;
        }
        int padding = (packet[0] & 32) != 0 ? packet[packet.Length - 1] : 0;
        if ((packet[0] & 32) != 0 && padding == 0) return false;
        length = packet.Length - offset - padding;
        return length > 0;
    }

    public void Stop()
    {
        ClientWebSocket active;
        lock (gate) { stopped = true; active = socket; }
        // Do not hold our state lock while cancellation dispatches socket callbacks.
        cancellation.Cancel();
        try { active?.Abort(); } catch (ObjectDisposedException) { }
    }
    public void Dispose() => Stop();
}

/// <summary>Only RTCP sender reports establish device UTC; receive time is never substituted.</summary>
public sealed class NeonRtpClock
{
    private bool valid;
    private uint source, anchorRtp;
    private double unixMs;
    private long reportTicks;
    private static uint U32(byte[] p, int i) => ((uint)p[i] << 24) | ((uint)p[i + 1] << 16) | ((uint)p[i + 2] << 8) | p[i + 3];

    public void ReadSenderReports(byte[] packet)
    {
        if (packet == null) return;
        for (int offset = 0; offset + 4 <= packet.Length;)
        {
            int length = (((packet[offset + 2] << 8) | packet[offset + 3]) + 1) * 4;
            if (packet[offset] >> 6 != 2 || length < 4 || offset + length > packet.Length) return;
            if (packet[offset + 1] == 200 && length >= 28)
            {
                uint seconds = U32(packet, offset + 8), fraction = U32(packet, offset + 12);
                if (seconds < 2208988800u) { valid = false; return; }
                source = U32(packet, offset + 4); anchorRtp = U32(packet, offset + 16);
                unixMs = (seconds - 2208988800u) * 1000.0 + fraction * (1000.0 / 4294967296.0);
                reportTicks = System.Diagnostics.Stopwatch.GetTimestamp(); valid = true;
            }
            offset += length;
        }
    }

    public bool TryMap(byte[] rtp, out long timestampMs)
    {
        timestampMs = 0;
        if (!valid || rtp == null || rtp.Length < 12 || U32(rtp, 8) != source ||
            (System.Diagnostics.Stopwatch.GetTimestamp() - reportTicks) / (double)System.Diagnostics.Stopwatch.Frequency > 45) return false;
        int delta = unchecked((int)(U32(rtp, 4) - anchorRtp));
        if (Math.Abs((double)delta) > 90000 * 45) return false;
        timestampMs = (long)Math.Round(unixMs + delta / 90.0);
        return timestampMs > 0;
    }
}

}
