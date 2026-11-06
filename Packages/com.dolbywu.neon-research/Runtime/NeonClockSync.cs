// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stopwatch = System.Diagnostics.Stopwatch;

/// <summary>Pupil Labs Time Echo (pl-realtime-api/time_echo.py).
/// Midpoint estimates assume symmetric transport; half RTT is not a guaranteed error bound.</summary>
public sealed class NeonClockSync : IDisposable
{
    private readonly object gate = new object();
    private readonly CancellationTokenSource cancel = new CancellationTokenSource();
    private readonly string ip;
    private readonly int apiPort;
    private readonly StreamWriter log;
    private double anchorSession, anchorPhoneMs, halfRtt;
    private long anchorTicks;
    private int anchorId;
    private bool disposed;
    public NeonClockSync(string ip, int apiPort, string folder)
    {
        this.ip = ip; this.apiPort = apiPort;
        log = new StreamWriter(Path.Combine(folder, "clock_sync.csv"));
        log.WriteLine("measurement_id,session_midpoint_s,pc_before_ms,pc_after_ms,phone_ms,rtt_ms,pc_minus_phone_ms,status");
        Task.Run(Run);
    }
    public static double Map(double deviceMs, double phoneMs, double sessionMidpoint)
        => sessionMidpoint + (deviceMs - phoneMs) / 1000.0;
    public bool TryMap(long deviceMs, bool rtcp, long receiveTicks, out double seconds, out double uncertaintyMs, out int id)
    {
        lock (gate)
        {
            seconds = 0; uncertaintyMs = halfRtt; id = anchorId;
            double age = (receiveTicks - anchorTicks) / (double)Stopwatch.Frequency;
            if (disposed || !rtcp || deviceMs <= 0 || anchorId == 0 || age < 0 || age > 45) return false;
            seconds = Map(deviceMs, anchorPhoneMs, anchorSession);
            double transportAge = StudyExportSession.SecondsAt(receiveTicks) - seconds;
            return seconds >= 0 && transportAge >= -0.1 && transportAge <= 5;
        }
    }
    private async Task Run()
    {
        int measurement = 0;
        while (!cancel.IsCancellationRequested)
        {
            try
            {
                var request = WebRequest.CreateHttp("http://" + ip + ":" + apiPort + "/api/status");
                request.Timeout = 2000; request.ReadWriteTimeout = 2000; request.Proxy = null;
                int port;
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var data = JObject.Parse(reader.ReadToEnd());
                    var token = data.SelectToken("$..time_echo_port");
                    port = token == null || token.Type == JTokenType.Null ? 0 : (int)token;
                }
                if (port < 1 || port > 65535) throw new IOException("time_echo_port_unavailable");
                using (var tcp = new TcpClient())
                using (cancel.Token.Register(() => tcp.Close()))
                {
                    var connect = tcp.ConnectAsync(ip, port);
                    if (await Task.WhenAny(connect, Task.Delay(2000, cancel.Token)) != connect) throw new IOException("connect_timeout");
                    await connect;
                    tcp.ReceiveTimeout = 2000; tcp.SendTimeout = 2000; tcp.NoDelay = true;
                    var stream = tcp.GetStream();
                    double best = double.MaxValue;
                    for (int i = 0; i < 11 && !cancel.IsCancellationRequested; i++)
                    {
                        long beforeTicks = Stopwatch.GetTimestamp();
                        long before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(before));
                        stream.Write(bytes, 0, 8);
                        byte[] response = new byte[16]; int read = 0;
                        while (read < 16) { int n = stream.Read(response, read, 16 - read); if (n == 0) throw new EndOfStreamException(); read += n; }
                        long after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        long afterTicks = Stopwatch.GetTimestamp();
                        long echo = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(response, 0));
                        long phone = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(response, 8));
                        double rtt = (afterTicks - beforeTicks) * 1000.0 / Stopwatch.Frequency;
                        double midpoint = StudyExportSession.SecondsAt(beforeTicks + (afterTicks - beforeTicks) / 2);
                        bool valid = i > 0 && echo == before && phone > 0 && rtt <= 100 && Math.Abs((after - before) - rtt) <= 10;
                        lock (gate)
                        {
                            if (disposed) return;
                            int id = ++measurement;
                            log.WriteLine(NeonStudyRecorder.Csv(id, midpoint, before, after, phone, rtt,
                                (before / 2.0 + after / 2.0) - phone, valid ? "valid_midpoint_estimate" : "rejected_or_warmup"));
                            log.Flush();
                            if (valid && rtt < best)
                            {
                                best = rtt; anchorSession = midpoint; anchorPhoneMs = phone;
                                anchorTicks = afterTicks; anchorId = id; halfRtt = rtt / 2 + 1;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                lock (gate)
                {
                    if (disposed) return;
                    log.WriteLine(NeonStudyRecorder.Csv(++measurement, "", "", "", "", "", "", "error_" + e.GetType().Name)); log.Flush();
                }
            }
            try { await Task.Delay(15000, cancel.Token); } catch (OperationCanceledException) { return; }
        }
    }
    public void Dispose()
    {
        lock (gate) { if (disposed) return; disposed = true; log.Dispose(); }
        cancel.Cancel();
    }
}

}
