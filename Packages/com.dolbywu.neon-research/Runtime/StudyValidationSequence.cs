using System;
using System.Linq;

namespace NeonResearch
{
    /// <summary>Deterministic presentation timing only; no gaze quality or calibration approval.</summary>
    public sealed class StudyValidationSequence
    {
        public int CurrentTarget { get; private set; } = -1;
        public int CompletedCount { get; private set; }
        public bool Started { get; private set; }
        public bool WaitingGap { get; private set; }
        public bool Finished { get; private set; }
        public double TargetStartedAt { get; private set; }
        private readonly int count;
        private readonly double dwell, gap;
        private int[] order;
        private double nextAt;
        public int[] Order => order == null ? new int[0] : (int[])order.Clone();

        public StudyValidationSequence(int count, double dwellSeconds, double gapSeconds)
        {
            if(count<1 || !Finite(dwellSeconds) || dwellSeconds<=0 || !Finite(gapSeconds) || gapSeconds<0)
                throw new ArgumentException("Invalid sequence count or timing.");
            this.count=count;dwell=dwellSeconds;gap=gapSeconds;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static int[] BuildOrder(int count,int first)
        {
            if(count<1 || first<0 || first>=count)throw new ArgumentOutOfRangeException(nameof(first));
            return new[]{first}.Concat(Enumerable.Range(0,count).Where(i=>i!=first)).ToArray();
        }
        public void Start(int first,double now)
        {
            if(Started || !Finite(now))throw new InvalidOperationException("Sequence already started or invalid clock.");
            order=BuildOrder(count,first);Started=true;CurrentTarget=order[0];TargetStartedAt=now;
        }
        public bool TargetDue(double now) => Started && !WaitingGap && !Finished && Finite(now) && now-TargetStartedAt>=dwell;
        public void FinishTarget(double now)
        {
            if(!TargetDue(now))throw new InvalidOperationException("Current target has not finished its dwell.");
            CompletedCount++;
            if(CompletedCount==count){Finished=true;return;}
            WaitingGap=true;nextAt=now+gap;
        }
        public bool TryStartNext(double now)
        {
            if(!WaitingGap || !Finite(now) || now<nextAt)return false;
            WaitingGap=false;CurrentTarget=order[CompletedCount];TargetStartedAt=now;return true;
        }
        public double GapRemaining(double now) => WaitingGap?Math.Max(0,nextAt-now):0;
        public double TargetRemaining(double now) => Started&&!WaitingGap&&!Finished?Math.Max(0,dwell-(now-TargetStartedAt)):0;
    }
}
