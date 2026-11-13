using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NeonResearch.Tests
{
    public sealed class ValidationReadinessTests
    {
        private static StudySpatialValidation.ValidationReport Ready()
        {
            var report=new StudySpatialValidation.ValidationReport{min_coverage=.7f,passed=false,correction_id="",targets=new List<StudySpatialValidation.TargetResult>()};
            for(int i=0;i<5;i++)report.targets.Add(new StudySpatialValidation.TargetResult{target_index=i,coverage=.9,local_direction_pairs=12,mean_error_deg=9+i,mean_observed_head_local_direction=new[]{0f,0f,1f},mean_expected_head_local_direction=new[]{0f,0f,1f}});
            return report;
        }
        [Test] public void ReadyHighErrorCollectionCanBeFittedWithoutClaimingAnAccuracyPass()
        {
            var report=Ready();Assert.That(StudyValidationReadiness.FitBlockers(report),Is.Empty);
            Assert.That(report.passed,Is.False);Assert.That(report.targets[0].mean_error_deg,Is.EqualTo(9));
            report.targets[0].coverage=.7;Assert.That(StudyValidationReadiness.FitBlockers(report),Is.Empty);
            report.targets[0].local_direction_pairs=11;Assert.That(StudyValidationReadiness.FitBlockers(report).Single(),Does.Contain("Point 1 (Centre): only 11"));
        }
        [Test] public void EveryInsufficientPointHasAnExplicitCoverageMessageWithoutThresholdRelaxation()
        {
            var report=Ready();var coverage=new[]{.868,.319,.055,.063,.715};
            for(int i=0;i<5;i++)report.targets[i].coverage=coverage[i];
            string[] issues=StudyValidationReadiness.FitBlockers(report);
            Assert.That(issues.Length,Is.EqualTo(3));
            Assert.That(issues[0],Does.Contain("Point 2 (Left)").And.Contain("31.9%").And.Contain("70.0%"));
            Assert.That(issues[1],Does.Contain("Point 3 (Right)").And.Contain("5.5%"));
            Assert.That(issues[2],Does.Contain("Point 4 (Up)").And.Contain("6.3%"));
            Assert.That(report.min_coverage,Is.EqualTo(.7f));Assert.That(report.targets[2].coverage,Is.EqualTo(.055));
        }
        [Test] public void MissingMalformedAndNonfiniteDirectionsNeverBecomeFitReady()
        {
            var report=Ready();report.targets[0].mean_observed_head_local_direction=null;
            report.targets[1].mean_expected_head_local_direction=new[]{0f,float.NaN,1f};
            report.targets[2].mean_observed_head_local_direction=new[]{0f,0f,0f};
            report.targets[3].mean_expected_head_local_direction=new[]{1f,2f};
            report.targets[4].mean_observed_head_local_direction=new[]{float.PositiveInfinity,0f,1f};
            var issues=StudyValidationReadiness.FitBlockers(report);Assert.That(issues.Length,Is.EqualTo(5));
            for(int i=0;i<5;i++)Assert.That(issues[i],Does.Contain("Point "+(i+1)).And.Contain("direction"));
        }
        [Test] public void MissingOrDuplicateIndicesCannotBorrowAnotherPointsMeasurements()
        {
            var report=Ready();report.targets[4].target_index=0;
            var issues=StudyValidationReadiness.FitBlockers(report);
            Assert.That(issues.Any(x=>x.Contains("duplicate")),Is.True);Assert.That(issues.Any(x=>x.Contains("Point 5 (Down): no unique")),Is.True);
            report=Ready();report.targets[4].target_index=99;Assert.That(StudyValidationReadiness.FitBlockers(report).Any(x=>x.Contains("invalid point index")),Is.True);
            report=Ready();report.targets.RemoveAt(4);Assert.That(StudyValidationReadiness.FitBlockers(report).Any(x=>x.Contains("exactly five")),Is.True);
            report=Ready();report.targets[3]=null;Assert.That(StudyValidationReadiness.FitBlockers(report).Any(x=>x.Contains("record 4 is missing")),Is.True);
        }
        [Test] public void NullAbortedCorrectedAndInvalidCoverageReportsAreBlocked()
        {
            Assert.That(StudyValidationReadiness.FitBlockers(null),Is.Not.Empty);
            var report=Ready();report.aborted=true;Assert.That(StudyValidationReadiness.FitBlockers(report).Any(x=>x.Contains("aborted")),Is.True);
            report=Ready();report.correction_id="prior-correction";Assert.That(StudyValidationReadiness.FitBlockers(report).Any(x=>x.Contains("already used")),Is.True);
            report=Ready();report.min_coverage=float.NaN;Assert.That(StudyValidationReadiness.FitBlockers(report).Any(x=>x.Contains("invalid minimum")),Is.True);
            foreach(double value in new[]{double.NaN,double.PositiveInfinity,-.1,1.1})
            {report=Ready();report.targets[2].coverage=value;Assert.That(StudyValidationReadiness.FitBlockers(report).Single(),Does.Contain("Point 3 (Right)").And.Contain("coverage is missing or invalid"));}
        }
    }
}
