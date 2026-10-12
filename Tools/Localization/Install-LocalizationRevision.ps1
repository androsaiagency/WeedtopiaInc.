[CmdletBinding()]
param([string]$UnityEditor='D:\Unity Games\6000.3.9f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))){throw 'Existing editor or lock: no second Unity process allowed.'}
$reportRoot=Join-Path $projectRoot 'Docs/LocalizationRevision/2026-10-06'
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
$arguments=@('-batchmode','-nographics','-projectPath',('"'+$projectRoot+'"'),'-logFile',('"'+(Join-Path $reportRoot 'Install.log')+'"'),'-executeMethod','Weedtopia.Editor.GameSetup.LocalizationRevisionValidation.Install','-quit')
$preserved=@{}
foreach($relative in @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')){
    $target=Join-Path $projectRoot $relative
    if(Test-Path -LiteralPath $target){$preserved[$target]=[IO.File]::ReadAllBytes($target)}
}
$started=[DateTime]::UtcNow
$owned=$null
try{
    $owned=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ('Owned localization installer Unity PID '+$owned.Id)
    $owned.WaitForExit()
}finally{
    foreach($target in $preserved.Keys){[IO.File]::WriteAllBytes($target,$preserved[$target])}
}
$receipt=Join-Path $reportRoot 'install.json'
if($owned.ExitCode -ne 0 -or !(Test-Path -LiteralPath $receipt) -or (Get-Item -LiteralPath $receipt).LastWriteTimeUtc -lt $started){throw 'Localization install failed; inspect Install.log.'}
Get-Content -LiteralPath $receipt
