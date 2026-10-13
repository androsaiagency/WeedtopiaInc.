[CmdletBinding()]
param(
    [ValidateSet('Install','Capture')][string]$Mode='Install',
    [string]$Label='',
    [string]$UnityEditor='D:\Unity Games\6000.3.9f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$reportRoot=Join-Path $projectRoot 'Docs/SaintMaryVille/Validation'
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
if((Get-Process Unity -ErrorAction SilentlyContinue)-or(Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))){throw 'Existing Unity editor or lock. No second editor is allowed.'}
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+$Mode+$(if($Label){'_'+$Label}else{''})
$log=Join-Path $reportRoot ($runId+'.log')
$method=if($Mode-eq'Install'){'Weedtopia.Editor.GameSetup.SaintMaryVilleAuthoring.Apply'}else{'Weedtopia.Editor.GameSetup.SaintMaryVilleEvidence.Begin'}
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
$receipt=Join-Path $projectRoot $(if($Mode-eq'Install'){'Docs/SaintMaryVille/authoring_receipt.json'}else{'Docs/SaintMaryVille/gameview_receipt.json'})
$result=[ordered]@{run=$runId;mode=$Mode;startedUtc=$started.ToString('o');endedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$process.ExitCode;log=$log;status='unverified'}
if((Test-Path -LiteralPath $receipt)-and(Get-Item -LiteralPath $receipt).LastWriteTimeUtc-ge$started){
    $data=Get-Content -LiteralPath $receipt -Raw|ConvertFrom-Json
    $valid=$process.ExitCode-eq0-and$data.status-eq'passed'
    if($Mode-eq'Capture'){
        $fresh=@($data.screenshots|Where-Object{!(Test-Path -LiteralPath $_)-or(Get-Item -LiteralPath $_).LastWriteTimeUtc-lt$started}).Count-eq0
        $layout=Join-Path $projectRoot 'Docs/SaintMaryVille/ui_overflow.json'
        $ui=if(Test-Path -LiteralPath $layout){Get-Content -LiteralPath $layout -Raw|ConvertFrom-Json}else{$null}
        $valid=$valid-and$fresh-and$data.screenshots.Count-eq83-and$data.savedStoryRoundtrip-and$data.housingInspectionMatchesSimulation-and$data.returnedToPreviousCity-and$data.allTenCitiesVisited-and$data.tenCityOfflineRoundtrip-and$data.eliasChoiceVariantsVerified-and$data.architectureGroundAnchorsVerified-and$data.matrix.Count-eq60-and$ui.passed
    }
    $result.status=if($valid){'passed'}else{'failed'};$result.receipt=$receipt
}
$result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $reportRoot ($runId+'.json')) -Encoding utf8
$result|ConvertTo-Json -Depth 5
if($result.status-ne'passed'){exit 1}
