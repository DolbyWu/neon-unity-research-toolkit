param([ValidateSet('Core','NeonXR','All')][string]$Package='All')
$ErrorActionPreference='Stop'
$repositoryRoot=Split-Path -Parent $PSScriptRoot
$artifacts=Join-Path $repositoryRoot 'Artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$artifacts=(Get-Item -LiteralPath $artifacts).FullName
$names=if($Package -eq 'Core'){@('com.dolbywu.neon-research')}elseif($Package -eq 'NeonXR'){@('com.binyanxu.gazeflow.neonxr')}else{@('com.dolbywu.neon-research','com.binyanxu.gazeflow.neonxr')}
$checksums=@()
foreach($name in $names){
    $packageRoot=Join-Path $repositoryRoot ('Packages/'+$name)
    $manifest=Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
    $stage=Join-Path $artifacts ('.package-stage-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    $stage=(Get-Item -LiteralPath $stage).FullName
    if(-not $stage.StartsWith($artifacts+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe staging path.'}
    $payload=Join-Path $stage 'package';New-Item -ItemType Directory -Path $payload | Out-Null
    try{
        foreach($folder in @('Runtime','Editor','Tests','Samples~','Documentation~','Tools~','Tools')){
            $source=Join-Path $packageRoot $folder
            if(-not(Test-Path -LiteralPath $source)){continue}
            foreach($entry in Get-ChildItem -LiteralPath $source -Recurse -Force){
                if($entry.LinkType -in @('SymbolicLink','Junction')){throw 'Linked package files are not allowed.'}
                $relative=[IO.Path]::GetRelativePath($source,$entry.FullName)
                if($relative -match '(^|[\\/])(Library|Temp|Logs|NeonConfig|UserSettings|Participants|__pycache__)([\\/]|$)' -or (-not $entry.PSIsContainer -and $entry.Extension -notin @('.cs','.meta','.asmdef','.txt','.py','.html','.md','.json','.prefab','.unity','.asset','.mat'))){throw "Unexpected package source: $relative"}
            }
            Copy-Item -LiteralPath $source -Destination $payload -Recurse
            if(Test-Path -LiteralPath ($source+'.meta')){Copy-Item -LiteralPath ($source+'.meta') -Destination $payload}
        }
        foreach($file in @('package.json','README.md','CHANGELOG.md','LICENSE.md','Third Party Notices.md')){
            $source=Join-Path $packageRoot $file
            if(Test-Path -LiteralPath $source){Copy-Item -LiteralPath $source -Destination $payload;if(Test-Path -LiteralPath ($source+'.meta')){Copy-Item -LiteralPath ($source+'.meta') -Destination $payload}}
        }
        foreach($entry in Get-ChildItem -LiteralPath $payload -Recurse -Force){
            $relative=[IO.Path]::GetRelativePath($payload,$entry.FullName)
            if($entry.Name.EndsWith('.meta') -or $relative -match '(^|[\\/])[^\\/]+~([\\/]|$)'){continue}
            if(-not(Test-Path -LiteralPath ($entry.FullName+'.meta'))){throw "Missing import metadata: $relative"}
        }
        $archive=Join-Path $artifacts ($manifest.name+'-'+$manifest.version+'.tgz')
        & tar -czf $archive -C $stage package
        if($LASTEXITCODE -ne 0){throw 'UPM archive creation failed.'}
        $hash=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant();$checksums+=($hash+'  '+[IO.Path]::GetFileName($archive));Write-Output "Created UPM beta: $archive"
    }finally{
        $checked=(Get-Item -LiteralPath $stage).FullName
        if($checked -ne $stage -or -not $checked.StartsWith($artifacts+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe staging cleanup.'}
        Remove-Item -LiteralPath $checked -Recurse -Force
    }
}
[IO.File]::WriteAllLines((Join-Path $artifacts 'SHA256SUMS.txt'),$checksums,[Text.UTF8Encoding]::new($false))
$checksums
