param([ValidateSet('Apply','Capture')][string]$Mode='Apply')
$ErrorActionPreference='Stop'
$project=(Get-Location).Path
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath (Join-Path $project 'Temp/UnityLockfile'))){throw 'Existing Unity editor or lock; not modified.'}
$report=Join-Path $PSScriptRoot 'Restore2D'
New-Item -ItemType Directory -Force -Path $report | Out-Null
$method=if($Mode -eq 'Apply'){'Weedtopia.Editor.GameSetup.TownHall2DRestore.Apply'}else{'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginTownHall2DRestore'}
$arguments=@('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $report ($Mode+'.log'))+'"'))
if($Mode -eq 'Apply'){$arguments+='-quit'}
$settings=@{}
foreach($path in @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')){if(Test-Path $path){$settings[$path]=[IO.File]::ReadAllBytes((Join-Path $project $path))}}
$started=[DateTime]::UtcNow
try {
    $process=Start-Process -FilePath 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ('Owned Unity '+$Mode+' PID: '+$process.Id)
    $process.WaitForExit();$process.Refresh()
    if($process.ExitCode -ne 0){throw ('Unity failed: '+$process.ExitCode)}
} finally {
    foreach($path in $settings.Keys){[IO.File]::WriteAllBytes((Join-Path $project $path),$settings[$path])}
}
$receipt=Join-Path $report $(if($Mode -eq 'Apply'){'restoration.json'}else{'gameview_receipt.json'})
if(!(Test-Path $receipt) -or (Get-Item $receipt).LastWriteTimeUtc -lt $started){throw 'Missing current receipt'}
Get-Content $receipt -Raw
