# Dependency and provenance review

## Licensing scope

See LICENSE.md for the plugin's MIT software licensing terms.

The MIT grant covers only material the copyright holder has authority to license. Third-party components and independently owned contributions retain their respective licenses; this notice does not relicense them. Demonstration photos, videos, participant recordings and research datasets are excluded from the software license and require separate permission for reuse. These materials are not included in the installable UPM package.

## Dependencies and provenance

- Historical research-source extraction hashes are retained outside the installable package in EXTRACTION_MANIFEST.json for maintainers. Provenance metadata is not a license grant for independently owned material. Preserve any applicable contributor notices.
- Unity JSON is declared as `com.unity.nuget.newtonsoft-json` 3.2.2 and resolved by Unity Package Manager. Its own package notices/licenses remain authoritative; DLLs are not copied into this package.
- Unity Test Framework/NUnit are development-only verification dependencies of the blank host, not bundled third-party executables.
- Neon wire layout documentation: https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/ . The preview decoder implements documented field order and big-endian numeric facts, not the original SDK's converted representation. Pupil Labs Unity SDK/native binaries are not redistributed. Physical and real-device validation remain pending.
- Python and FFmpeg are optional operator-installed executables, not included. If a future release redistributes them, their exact builds, licenses and store-policy implications require separate review. The copied capture/archive helpers are project research source, not these external tools.
- No Meta SDK/game assets, OpenAI client, MCP plugin, fonts, participant recordings or identities are bundled.

The project is unofficial. Neither the MIT license nor protocol compatibility implies endorsement by Unity or Pupil Labs. Future third-party additions require their own license review and attribution.
