[CmdletBinding()]
param(
    [ValidateSet('Install','Capture')][string]$Mode='Install',
    [string]$Label='',
    [string]$UnityEditor='D:\Unity Games\6000.3.9f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$reportRoot=Join-Path $projectRoot 'Docs/PurpleGarden/Validation'
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
if((Get-Process Unity -ErrorAction SilentlyContinue)-or(Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))){throw 'Existing Unity editor or lock. No second editor is allowed.'}
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+$Mode+$(if($Label){'_'+$Label}else{''})
$log=Join-Path $reportRoot ($runId+'.log')
$method=if($Mode-eq'Install'){'Weedtopia.Editor.GameSetup.PurpleGardenAuthoring.Apply'}else{'Weedtopia.Editor.GameSetup.PurpleGardenEvidence.Begin'}
$arguments=@('-batchmode','-projectPath',('"'+$projectRoot+'"'),'-logFile',('"'+$log+'"'),'-executeMethod',$method)
if($Mode-eq'Install'){$arguments+=@('-nographics','-quit')}
$settings=@{}
foreach($relative in @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')){
    $path=Join-Path $projectRoot $relative
    if(Test-Path -LiteralPath $path){$settings[$path]=[IO.File]::ReadAllBytes($path)}
}
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned Unity PID '+$process.Id+'; '+$runId)
try{$process.WaitForExit()}finally{foreach($path in $settings.Keys){[IO.File]::WriteAllBytes($path,$settings[$path])}}
$receipt=Join-Path $projectRoot $(if($Mode-eq'Install'){'Docs/PurpleGarden/authoring_receipt.json'}else{'Docs/PurpleGarden/gameview_receipt.json'})
$result=[ordered]@{run=$runId;mode=$Mode;startedUtc=$started.ToString('o');endedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$process.ExitCode;log=$log;status='unverified'}
if((Test-Path -LiteralPath $receipt)-and(Get-Item -LiteralPath $receipt).LastWriteTimeUtc-ge$started){
    $result.receipt=$receipt;$data=Get-Content -LiteralPath $receipt -Raw|ConvertFrom-Json
    if($Mode-eq'Install'){$result.status=if($process.ExitCode-eq0-and$data.status-eq'passed'){'passed'}else{'failed'}}
    else{
        $layout=Join-Path $projectRoot 'Docs/PurpleGarden/ui_overflow.json'
        $fresh=@($data.screenshots|Where-Object{!(Test-Path -LiteralPath $_)-or(Get-Item -LiteralPath $_).LastWriteTimeUtc-lt$started}).Count-eq0
        $ui=if(Test-Path -LiteralPath $layout){Get-Content -LiteralPath $layout -Raw|ConvertFrom-Json}else{$null}
        $expectedShots=$data.expectedScreenshots
        $planned=@($data.plannedCaptures).Count-eq$expectedShots-and$expectedShots-gt0
        $dimensions=@($data.captureDimensions).Count-eq$expectedShots-and@($data.captureDimensions|Where-Object{$_.pngWidth-ne$_.requestedWidth-or$_.pngHeight-ne$_.requestedHeight-or$_.renderedWidth-ne$_.requestedWidth-or$_.renderedHeight-ne$_.requestedHeight}).Count-eq0
        $saveOffline=$data.saveOffline.passed-and$data.saveOffline.loadedCities-eq8-and$data.saveOffline.offlineSeconds-eq64-and$data.saveOffline.allCitiesProduced-and$data.saveOffline.assignedWorkersPreserved-and$data.saveOffline.choiceMemoryPreserved-and$data.saveOffline.nextOfflineReplayWasZero
        $preserved=@($data.preservedPreviousCities).Count-eq7
        $result.status=if($process.ExitCode-eq0-and$data.mode-eq'actual_playmode_gameview'-and$data.screenshots.Count-eq$expectedShots-and$fresh-and$planned-and$dimensions-and$saveOffline-and$preserved-and$ui.passed-and$ui.fingerprint-eq$data.fingerprint-and(Get-Item -LiteralPath $layout).LastWriteTimeUtc-ge$started){'passed'}else{'failed'}
        $result.screenshots=$data.screenshots.Count;$result.layout=$layout;$result.saveOffline=$data.saveOffline;$result.preservedPreviousCities=$data.preservedPreviousCities
    }
}
$result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $reportRoot ($runId+'.json')) -Encoding utf8
$result|ConvertTo-Json -Depth 5
if($result.status-ne'passed'){exit 1}
