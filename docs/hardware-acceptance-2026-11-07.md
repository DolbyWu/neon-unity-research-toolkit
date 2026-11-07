# Hardware test results — 7 November 2026

**Result: core-function real-device tests passed. Ready for public preview and pilot use.**

Test environment: Windows, Unity 6000.3.23f1, Pupil Labs Neon with Companion, Meta Quest 3 through Link. The independent VR host used OpenXR 1.16.1 and XR Management 4.6.1, with head and interaction sources connected explicitly.

| Function | Result |
| --- | --- |
| Independent installation and component scene persistence | Passed |
| Real Neon gaze and left/right pupil reception | Passed |
| Live eye-data monitor and local CSV export | Passed |
| Real Quest head position and quaternion rotation logging | Passed |
| Explicit left/right grip and release logging | Passed |
| Local Quest-view video and microphone capture/finalization | Passed |
| Finished-session folder and offline HTML export | Passed |
| Local video playback, pause and scrubbing in HTML | Passed |
| Current-frame video/experiment timestamps | Passed |
| Multiple annotations and JSON export/import regression checks | Passed |

The tested signed preview.4 archive has SHA-256 `293cf019a2f168cad88724346b32f49c150112318c512b521c2a835688bb5022`. Software checks passed: **26/26 Unity acceptance cases**, **2/2 offline browser suites** and **14/14 synthetic Python tests**. Signed payload integrity and fresh import were checked separately.

Functional acceptance is not physical pupil calibration or hardware time synchronization. Pupil values use documented wire units; Time Echo and media offsets are estimates. The host project supplies its own XR setup and explicit interaction callbacks. No gaze-to-object mapping or automatic cognitive-state classifier is included.

Only results are published. Raw recordings, participant profiles, device/network identifiers and detailed trial records remain local. See [release notes](../RELEASE_NOTES.md) and [Validation](../VALIDATION.md).
