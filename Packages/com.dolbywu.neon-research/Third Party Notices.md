# Dependency and provenance review

This is a private development preview, not a completed legal clearance.

- The implementation includes pre-existing research source contributions. Historical source hashes are retained outside the installable package in EXTRACTION_MANIFEST.json for maintainers. Source/contributor redistribution rights must be confirmed before publication.
- Unity JSON is declared as `com.unity.nuget.newtonsoft-json` 3.2.2 and resolved by Unity Package Manager. Its own package notices/licenses remain authoritative; DLLs are not copied into this package.
- Unity Test Framework/NUnit are development-only verification dependencies of the blank host, not bundled third-party executables.
- Neon wire layout documentation: https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/ . The preview decoder implements documented field order and big-endian numeric facts, not the original SDK's converted representation. Pupil Labs Unity SDK/native binaries are not redistributed. Physical and real-device validation remain pending.
- Python and FFmpeg are optional operator-installed executables, not included. If a future release redistributes them, their exact builds, licenses and store-policy implications require separate review. The copied capture/archive helpers are project research source, not these external tools.
- No Meta SDK/game assets, OpenAI client, MCP plugin, fonts, participant recordings or identities are bundled.

Using a protocol or declaring a dependency does not itself establish rights to publish this whole derivative package. Resolve this review before any public GitHub/Asset Store release.
