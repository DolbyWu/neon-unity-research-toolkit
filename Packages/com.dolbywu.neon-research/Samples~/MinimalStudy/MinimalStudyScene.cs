using UnityEngine;
using NeonResearch;

public sealed class MinimalStudyScene:MonoBehaviour
{
    // This sample creates synthetic game events only. It never fabricates gaze/pupil data.
    public void MarkSyntheticAttempt()=>StudyEvents.Record("sample_attempt","sample_cube","","Synthetic demonstration only");
    public void MarkSyntheticSuccess()=>StudyEvents.Record("sample_success","sample_cube","success","Synthetic demonstration only");
}
