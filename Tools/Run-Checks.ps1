param(
    [string]$UnityExecutable='C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe',
    [string]$NodeExecutable='node',
    [string]$PlaywrightModule='playwright',
    [string]$PythonExecutable='python',
    [string]$FfmpegExecutable
)
$ErrorActionPreference='Stop'
$standaloneRoot=Split-Path -Parent $PSScriptRoot
$hostProject=(Get-Item -LiteralPath (Join-Path $standaloneRoot 'VerificationProject')).FullName
$arguments='-batchmode -nographics -projectPath "'+$hostProject+'" -runTests -testPlatform editmode -testFilter "NeonResearch.Tests" -testResults "'+$hostProject+'\standalone-tests.xml" -logFile "'+$hostProject+'\standalone-tests.log"'
Write-Output 'Testing only the separate VerificationProject. No original project is opened or closed.'
$process=Start-Process -FilePath $UnityExecutable -ArgumentList $arguments -PassThru -WindowStyle Hidden -Wait
if ($process.ExitCode -ne 0) { throw "Independent Unity tests exited $($process.ExitCode). Inspect VerificationProject/standalone-tests.log." }
$env:STUDY_PLAYWRIGHT=$PlaywrightModule
if (-not [string]::IsNullOrWhiteSpace($FfmpegExecutable)) { $env:STUDY_FFMPEG=$FfmpegExecutable }
Push-Location -LiteralPath $standaloneRoot
try {
    & $NodeExecutable 'Packages/com.dolbywu.neon-research/Verification/Test-OfflineReplay.cjs'
    if ($LASTEXITCODE -ne 0) { throw 'Offline browser checks failed.' }
    if (-not [string]::IsNullOrWhiteSpace($FfmpegExecutable)) {
        & $NodeExecutable 'Packages/com.dolbywu.neon-research/Verification/Test-ReplayAnnotations.cjs'
        if ($LASTEXITCODE -ne 0) { throw 'Synthetic video/annotation browser checks failed.' }
        & $PythonExecutable -m unittest discover -s 'Packages/com.dolbywu.neon-research/Tools/StudyArchive' -p 'test_*.py' -v
        if ($LASTEXITCODE -ne 0) { throw 'Synthetic Python checks failed.' }
    } else { Write-Output 'SKIPPED: video/annotation and Python media checks require an operator-provided FFmpeg executable.' }
} finally { Pop-Location }
Write-Output 'Complete. Check XML/logs; synthetic checks do not validate real Neon/media capture.'
