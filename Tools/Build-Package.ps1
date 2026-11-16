$ErrorActionPreference='Stop'
$standaloneRoot=Split-Path -Parent $PSScriptRoot
$packageRoot=(Get-Item -LiteralPath (Join-Path $standaloneRoot 'Packages/com.dolbywu.neon-research')).FullName
$package=Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
$artifacts=Join-Path $standaloneRoot 'Artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$artifacts=(Get-Item -LiteralPath $artifacts).FullName
$stage=Join-Path $artifacts ('.package-stage-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$stage=(Get-Item -LiteralPath $stage).FullName
if (-not $stage.StartsWith($artifacts+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Staging path escaped Artifacts.' }
$payload=Join-Path $stage 'package'
New-Item -ItemType Directory -Path $payload | Out-Null
try {
    foreach ($name in @('Runtime','Editor','Tests','Samples~','Documentation~','Tools~')) {
        $source=Join-Path $packageRoot $name
        if (Test-Path -LiteralPath $source) {
            # OneDrive cloud placeholders can have ReparsePoint without being path links.
            # Reject actual filesystem links/junctions; do not reject hydrated regular source files.
            if (Get-ChildItem -LiteralPath $source -Recurse -Force | Where-Object { $_.LinkType -in @('SymbolicLink','Junction') }) { throw 'Linked package files are not allowed.' }
            Copy-Item -LiteralPath $source -Destination $payload -Recurse
        }
    }
    foreach ($name in @('package.json','README.md','CHANGELOG.md','LICENSE.md','Third Party Notices.md','Runtime.meta','Editor.meta','Tests.meta','Tools.meta')) {
        $source=Join-Path $packageRoot $name
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination $payload
            if (-not $name.EndsWith('.meta') -and (Test-Path -LiteralPath ($source+'.meta'))) {
                Copy-Item -LiteralPath ($source+'.meta') -Destination $payload
            }
        }
    }
    # Explicit source whitelist: no interpreter/FFmpeg binaries, pycache, live settings or generated data.
    $toolDestination=Join-Path $payload 'Tools/StudyArchive'
    New-Item -ItemType Directory -Path $toolDestination -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $packageRoot 'Tools/StudyArchive.meta') -Destination (Join-Path $payload 'Tools/StudyArchive.meta')
    Get-ChildItem -LiteralPath (Join-Path $packageRoot 'Tools/StudyArchive') -File |
        Where-Object { $_.Extension -in @('.py','.html','.meta') } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $toolDestination }
    # Immutable UPM packages cannot create missing metadata during import.
    foreach ($entry in Get-ChildItem -LiteralPath $payload -Recurse -Force) {
        $relative=[IO.Path]::GetRelativePath($payload,$entry.FullName)
        if ($entry.Name.EndsWith('.meta') -or $relative -match '(^|[\\/])[^\\/]+~([\\/]|$)') { continue }
        if (-not (Test-Path -LiteralPath ($entry.FullName+'.meta'))) { throw "Missing import metadata: $relative" }
    }
    $archive=Join-Path $artifacts ($package.name+'-'+$package.version+'.tgz')
    & tar -czf $archive -C $stage package
    if ($LASTEXITCODE -ne 0) { throw 'Package archive creation failed.' }
    $inventory=@(& tar -tzf $archive)
    if ($LASTEXITCODE -ne 0 -or ($inventory | Where-Object { $_ -match '(^|/)(Library|NeonConfig|UserSettings|Participants|__pycache__|Verification)/|\.pyc$' })) { throw 'Unexpected package inventory.' }
    Write-Output "Created local UPM tarball: $archive"
    Get-FileHash -LiteralPath $archive -Algorithm SHA256 | Format-List
} finally {
    # Only this newly created, resolved staging directory is removed; original projects are never targets.
    $checked=(Get-Item -LiteralPath $stage).FullName
    if ($checked -ne $stage -or -not $checked.StartsWith($artifacts+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe temporary cleanup path.' }
    Remove-Item -LiteralPath $checked -Recurse -Force
}
