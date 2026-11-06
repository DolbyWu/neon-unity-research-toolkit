using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Stopwatch=System.Diagnostics.Stopwatch;

namespace NeonResearch
{
    /// <summary>Opt-in managed Neon receiver. No native RTSP library, gaze-object mapping or inferred fixations.</summary>
    public sealed class NeonStudyRecorder:MonoBehaviour
    {
        private static NeonStudyRecorder instance;
        private readonly object gate=new object();
        private readonly ConcurrentQueue<Packet> queue=new ConcurrentQueue<Packet>();
        private CancellationTokenSource cancellation;
        private NeonTcpGazeClient client;
        private NeonClockSync clock;
        private NeonStudySettings settings;
        private StreamWriter gaze,payloads;
        private string folder;
        private volatile string status="not_started",lastError="";
        private bool closed;
        private long received,written,dropped,unsupported,lastTimestamp;
        private int epoch,lastEpoch=-1;
        private float nextFlush;
        private struct Packet{public byte[] bytes;public long timestamp,ticks,utc;public bool rtcp;public int epoch;}
        public static string MonitorStatus=>instance==null?"Disabled / not started":instance.status;
        public static long MonitorDropped=>instance==null?0:Interlocked.Read(ref instance.dropped);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic(){instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var config=NeonStudySettings.Load();
            if(!config.recordOnPlay||!config.enabled||StudyExportSession.Folder==null)return;
            var go=new GameObject("Neon Research receiver");DontDestroyOnLoad(go);instance=go.AddComponent<NeonStudyRecorder>();
        }
        private void Start()
        {
            try
            {
                settings=NeonStudySettings.Load();
                folder=Path.Combine(StudyExportSession.Folder,"Neon-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(folder);
                gaze=new StreamWriter(Path.Combine(folder,"NeonGaze.csv"),false,new UTF8Encoding(false));
                gaze.WriteLine("sample_id,connection_epoch,device_timestamp_ms,rtcp_synchronized,receive_utc_ms,receive_monotonic_ticks,process_monotonic_ticks,unity_frame,data_type,worn,gaze_x_px,gaze_y_px,eye_state_available,pupil_left_mm,pupil_right_mm,mapping_status,receive_session_time_s,process_session_time_s,aligned_session_time_s,time_alignment_status,clock_measurement_id,clock_half_rtt_plus_resolution_ms,sample_quality,dual_left_x_px,dual_left_y_px,dual_right_x_px,dual_right_y_px,pupil_units");
                payloads=new StreamWriter(Path.Combine(folder,"raw_payloads.csv"),false,new UTF8Encoding(false));
                payloads.WriteLine("sample_id,connection_epoch,receive_utc_ms,device_timestamp_ms,rtcp_synchronized,payload_length,payload_base64");
                if(!IPAddress.TryParse(settings.companionIp,out var address)||address.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork)
                {status="waiting_for_valid_companion_ipv4";Flush();return;}
                cancellation=new CancellationTokenSource();clock=new NeonClockSync(settings.companionIp,settings.companionApiPort,folder);
                _=Connect(cancellation.Token);
            }
            catch(Exception e){Fail(e);}
        }
        private async Task Connect(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                try
                {
                    status="connecting";int connectionEpoch=++epoch;
                    using(var receiver=new NeonTcpGazeClient(settings.companionIp,settings.websocketPort,(bytes,timestamp,rtcp)=>
                    {
                        lock(gate)
                        {
                            if(closed||token.IsCancellationRequested)return;
                            Interlocked.Increment(ref received);
                            if(queue.Count>=8192){Interlocked.Increment(ref dropped);return;}
                            queue.Enqueue(new Packet{bytes=bytes,timestamp=timestamp,rtcp=rtcp,epoch=connectionEpoch,
                                ticks=Stopwatch.GetTimestamp(),utc=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()});
                        }
                    },s=>{if(!closed&&!token.IsCancellationRequested)status=s;}))
                    {client=receiver;if(token.IsCancellationRequested)receiver.Stop();await receiver.RunAsync().ConfigureAwait(false);}
                    if(!token.IsCancellationRequested)status="disconnected_retrying";
                }
                catch(Exception e){if(token.IsCancellationRequested)break;lastError=e.GetType().Name+": "+e.Message;status="connection_error_retrying";}
                finally{client=null;}
                try{await Task.Delay(3000,token).ConfigureAwait(false);}catch(OperationCanceledException){break;}
            }
        }
        private void Update()
        {
            if(closed||gaze==null)return;
            try{int budget=2048;while(budget-->0&&queue.TryDequeue(out var p))Write(p);if(Time.unscaledTime>=nextFlush){nextFlush=Time.unscaledTime+2;Flush();}}
            catch(Exception e){Fail(e);}
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        private static object Cell(float value)=>Finite(value)?(object)value:"";
        private void Write(Packet p)
        {
            var d=NeonWireDecoder.Decode(p.bytes);if(!d.supported)unsupported++;
            bool ordered=p.epoch!=lastEpoch||p.timestamp>lastTimestamp;
            string quality=!d.supported?"unsupported_payload_length":!d.worn?"not_worn":!d.binocular?"dual_monocular_only_no_binocular_value":
                !Finite(d.x)||!Finite(d.y)?"invalid_gaze":p.rtcp&&!ordered?"duplicate_or_out_of_order":"basic_valid_not_full_eye_quality_assessment";
            double aligned=0,uncertainty=0;int measurement=0;
            bool synced=clock!=null&&clock.TryMap(p.timestamp,p.rtcp,p.ticks,out aligned,out uncertainty,out measurement);
            written++;
            payloads.WriteLine(Csv(written,p.epoch,p.utc,p.timestamp,p.rtcp,p.bytes.Length,Convert.ToBase64String(p.bytes)));
            gaze.WriteLine(Csv(written,p.epoch,p.timestamp,p.rtcp,p.utc,p.ticks,Stopwatch.GetTimestamp(),Time.frameCount,"wire_"+p.bytes.Length,d.worn,
                Cell(d.x),Cell(d.y),d.eyeState,d.eyeState?Cell(d.leftPupil):"",d.eyeState?Cell(d.rightPupil):"","mapping_not_implemented",
                StudyExportSession.SecondsAt(p.ticks),StudyExportSession.Seconds,synced?(object)aligned:"",synced?"time_echo_estimated":"unavailable_not_receive_time",
                synced?(object)measurement:"",synced?(object)uncertainty:"",quality,Cell(d.leftX),Cell(d.leftY),Cell(d.rightX),Cell(d.rightY),"mm_documented_wire_protocol_NOT_physical_validation"));
            StudyMonitorData.Sample(StudyExportSession.SecondsAt(p.ticks),d.x,d.y,d.leftPupil,d.rightPupil,d.eyeState,
                quality=="basic_valid_not_full_eye_quality_assessment",synced,p.epoch);
            if(ordered){lastEpoch=p.epoch;lastTimestamp=p.timestamp;}
            if(!closed)status="receiving_"+quality;
        }
        private void Flush()
        {
            if(folder==null)return;gaze?.Flush();payloads?.Flush();
            StudyAtomicFile.Write(Path.Combine(folder,"session.json"),JsonUtility.ToJson(new Metadata{status=status,received=Interlocked.Read(ref received),written=written,
                queueDropped=Interlocked.Read(ref dropped),queued=queue.Count,unsupportedPayloads=unsupported,closed=closed,lastConnectionError=lastError},true));
        }
        [Serializable]private sealed class Metadata
        {
            public string schemaVersion="standalone-preview-1",source="neon_documented_wire_managed_websocket",status,lastConnectionError;
            public string decoderValidation="synthetic_wire_packets_only_real_device_revalidation_required";
            public string pupilUnits="mm_from_documented_wire_protocol_NOT_physical_validation";
            public string poseTiming="Unity_world_pose_logger_call_time_NOT_hardware_synchronized";
            public bool closed,mappingCalibrationVerified=false;
            public long received,written,queueDropped,unsupportedPayloads;public int queued;
        }
        private void Fail(Exception e){StudyExportSession.ReportFailure("Neon receiver error: "+e.Message);lastError=e.Message;status="error";Close();enabled=false;}
        private void Close()
        {
            lock(gate){if(closed)return;closed=true;}
            cancellation?.Cancel();client?.Stop();status="closed";
            try{while(queue.TryDequeue(out var p))Write(p);Flush();}
            catch(Exception e){StudyExportSession.ReportFailure("Neon finalization failed: "+e.Message);}
            finally{gaze?.Dispose();payloads?.Dispose();clock?.Dispose();}
            StudySequenceAnalysis.TryBuildClosed(StudyExportSession.Folder);
        }
        private void OnApplicationQuit()=>Close();private void OnDestroy()=>Close();
        public static string Csv(params object[] values)
        {
            var fields=new string[values.Length];
            for(int i=0;i<values.Length;i++)
            {
                string text=Convert.ToString(values[i],CultureInfo.InvariantCulture)??"";
                if(values[i] is string&&text.Length>0&&"=+-@\t\r".IndexOf(text[0])>=0)text="'"+text;
                fields[i]="\""+text.Replace("\"","\"\"")+"\"";
            }
            return string.Join(",",fields);
        }
    }
}
