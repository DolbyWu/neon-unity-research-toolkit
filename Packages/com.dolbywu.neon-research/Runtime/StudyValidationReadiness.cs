using System;
using System.Collections.Generic;
using System.Globalization;

namespace NeonResearch
{
    /// <summary>Collection quality only. Large angular error is not a fitting blocker or an accuracy pass.</summary>
    public static class StudyValidationReadiness
    {
        private static readonly string[] Names={"Centre","Left","Right","Up","Down"};
        public static string[] FitBlockers(StudySpatialValidation.ValidationReport report)
        {
            var blockers=new List<string>();
            if(report==null)return new[]{"Round 1 has no completed report. Collect all five points first."};
            if(report.aborted)blockers.Add("Round 1 was aborted. Collect a complete new five-point round before fitting.");
            if(!string.IsNullOrEmpty(report.correction_id))blockers.Add("This report already used a correction. Fitting requires a new uncorrected round 1.");
            bool validThreshold=Finite(report.min_coverage)&&report.min_coverage>=0&&report.min_coverage<=1;
            if(!validThreshold)blockers.Add("Round 1 has an invalid minimum coverage setting. Use a finite value from 0 to 1 and collect again.");
            if(report.targets==null)return Append(blockers,"Round 1 has no target records. Collect all five points first.");
            if(report.targets.Count!=5)blockers.Add("Round 1 requires exactly five target records; found "+report.targets.Count+". Collect all five points in one new round.");
            var seen=new HashSet<int>();
            for(int n=0;n<report.targets.Count;n++)
            {
                var target=report.targets[n];
                if(target==null){blockers.Add("Target record "+(n+1)+" is missing. Collect a complete new round.");continue;}
                int index=target.target_index;
                if(index<0||index>=5){blockers.Add("Target record "+(n+1)+" has an invalid point index. Expected five unique indices 0 to 4.");continue;}
                string point="Point "+(index+1)+" ("+Names[index]+"): ";
                if(!seen.Add(index))blockers.Add(point+"duplicate target record. Do not combine measurements from different rounds.");
                if(!Finite(target.coverage)||target.coverage<0||target.coverage>1)
                    blockers.Add(point+"valid-data coverage is missing or invalid; collect this point again in a new round.");
                else if(validThreshold&&target.coverage<report.min_coverage)
                    blockers.Add(point+"valid-data coverage "+Percent(target.coverage)+"; requires at least "+Percent(report.min_coverage)+". Check live worn data and repeat round 1.");
                if(target.local_direction_pairs<12)
                    blockers.Add(point+"only "+target.local_direction_pairs+" valid direction pairs; at least 12 are required. Missing samples are not fitted.");
                if(!Direction(target.mean_observed_head_local_direction))
                    blockers.Add(point+"observed head-local direction is missing, nonfinite or too small to fit. Repeat round 1.");
                if(!Direction(target.mean_expected_head_local_direction))
                    blockers.Add(point+"expected head-local direction is missing, nonfinite or too small to fit. Check the target/head binding and repeat round 1.");
            }
            for(int i=0;i<5;i++)if(!seen.Contains(i))blockers.Add("Point "+(i+1)+" ("+Names[i]+"): no unique target record in this round.");
            return blockers.ToArray();
        }
        private static string[] Append(List<string> values,string value){values.Add(value);return values.ToArray();}
        private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        private static string Percent(double value)=>(value*100).ToString("F1",CultureInfo.InvariantCulture)+"%";
        private static bool Direction(float[] vector)
        {
            if(vector==null||vector.Length!=3)return false;
            double square=0;
            foreach(float value in vector){if(!Finite(value))return false;square+=(double)value*value;}
            // Match the bounded direction fitter's minimum vector magnitude.
            return square>=.01;
        }
    }
}
