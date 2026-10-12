$ErrorActionPreference='Stop'
$project=(Get-Location).Path
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity already running; no second editor will be started.' }
if (Test-Path -LiteralPath (Join-Path $project 'Temp/UnityLockfile')) { throw 'Unity lock exists; not modified.' }
$report=Join-Path $PSScriptRoot 'UnityReview'
New-Item -ItemType Directory -Force -Path $report | Out-Null
$arguments=@('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod','Weedtopia.Editor.BuildingArtPipeline.TownHall3DReview.Run','-quit','-logFile',('"'+(Join-Path $report 'unity.log')+'"'))
$process=Start-Process -FilePath 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned Unity review PID: '+$process.Id)
$process.WaitForExit()
$process.Refresh()
Write-Output ('Unity exit: '+$process.ExitCode)
if ($process.ExitCode -ne 0) { exit 1 }
if (-not (Test-Path (Join-Path $report 'unity_review.json'))) { throw 'No review receipt.' }
Get-Content -LiteralPath (Join-Path $report 'unity_review.json') -Raw
