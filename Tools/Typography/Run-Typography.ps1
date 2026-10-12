param([ValidateSet('Install','Capture')][string]$Mode='Install')
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))){throw 'Existing editor or lock: no second Unity process allowed.'}
$method=if($Mode -eq 'Install'){'Weedtopia.Editor.GameSetup.TypographyAuthoring.Apply'}else{'Weedtopia.Editor.GameSetup.LocalizationEvidence.BeginTypography'}
$folder=Join-Path $projectRoot 'Docs/Typography'
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$log=Join-Path $folder ($Mode+'.log')
$arguments=@('-batchmode','-projectPath',('"'+$projectRoot+'"'),'-logFile',('"'+$log+'"'),'-executeMethod',$method)
if($Mode -eq 'Install'){$arguments+=@('-nographics','-quit')}
$preserved=@{}
foreach($relative in @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')){
 $target=Join-Path $projectRoot $relative;if(Test-Path -LiteralPath $target){$preserved[$target]=[IO.File]::ReadAllBytes($target)}
}
$owned=Start-Process 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned typography Unity PID '+$owned.Id)
$owned.WaitForExit()
foreach($target in $preserved.Keys){[IO.File]::WriteAllBytes($target,$preserved[$target])}
Write-Output ('Unity exit '+$owned.ExitCode)
if($owned.ExitCode -ne 0){throw ('Unity typography '+$Mode+' failed. Inspect '+$log)}
