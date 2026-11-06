param([string]$SourceRoot)
$ErrorActionPreference='Stop'
$standaloneRoot=Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($SourceRoot)) { $SourceRoot=Join-Path (Split-Path -Parent $standaloneRoot) 'EscapeRoom-Neon-Publish-20261014' }
$source=(Get-Item -LiteralPath $SourceRoot).FullName
$manifest=Get-Content -LiteralPath (Join-Path $standaloneRoot 'EXTRACTION_MANIFEST.json') -Raw | ConvertFrom-Json
$differences=@(foreach ($entry in $manifest.files) {
    $path=Join-Path $source $entry.path
    if (-not (Test-Path -LiteralPath $path)) { [pscustomobject]@{path=$entry.path;status='missing'}; continue }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256) { [pscustomobject]@{path=$entry.path;status='changed'} }
})
if ($differences.Count) { $differences | Format-Table; throw 'Selected original source files differ from the extraction baseline. Nothing was restored or overwritten.' }
Write-Output "PASS: all $($manifest.files.Count) selected original source files match the extraction baseline. This script only reads originals."
