param([ValidateSet('Install','Capture')][string]$Mode='Install')
$ErrorActionPreference='Stop'
$project=(Get-Location).Path
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath (Join-Path $project 'Temp/UnityLockfile'))){throw 'Existing Unity editor or lock; not modified.'}
$report=Join-Path $PSScriptRoot 'CityIntegration'
$method=if($Mode -eq 'Install'){'Weedtopia.Editor.GameSetup.TownHall3DIntegration.Apply'}else{'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginTownHall'}
$arguments=@('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $report ($Mode+'.log'))+'"'))
if($Mode -eq 'Install'){$arguments+='-quit'}
$settings=@{}
foreach($path in @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')){if(Test-Path $path){$settings[$path]=[IO.File]::ReadAllBytes((Join-Path $project $path))}}
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned Unity '+$Mode+' PID: '+$process.Id)
$process.WaitForExit();$process.Refresh()
foreach($path in $settings.Keys){[IO.File]::WriteAllBytes((Join-Path $project $path),$settings[$path])}
if($process.ExitCode -ne 0){throw ('Unity failed: '+$process.ExitCode)}
$receipt=Join-Path $report $(if($Mode -eq 'Install'){'integration.json'}else{'gameview_receipt.json'})
if(!(Test-Path $receipt) -or (Get-Item $receipt).LastWriteTimeUtc -lt $started){throw 'Missing current receipt'}
Get-Content $receipt -Raw
