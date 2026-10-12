[CmdletBinding()]
param(
    [ValidateSet('Install','Refine','Capture')][string]$Mode = 'Install',
    [string]$Label = '',
    [string]$UnityEditor = 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$reportRoot = Join-Path $projectRoot 'Docs/VipStories/Validation'
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))) {
    throw 'Existing Unity editor or lock. No second editor is allowed.'
}
$runId = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + $Mode + $(if($Label){'_'+$Label}else{''})
$log = Join-Path $reportRoot ($runId + '.log')
$method = switch($Mode){'Install'{'Weedtopia.Editor.GameSetup.VipStoriesAuthoring.Apply'}'Refine'{'Weedtopia.Editor.GameSetup.VipStoriesAuthoring.RefinePresentation'}'Capture'{'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginVipStories'}}
$arguments = @('-batchmode','-projectPath',('"'+$projectRoot+'"'),'-logFile',('"'+$log+'"'),'-executeMethod',$method)
if($Mode -ne 'Capture'){$arguments += @('-nographics','-quit')}
$settings = @{}
foreach($relative in @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')) {
    $path = Join-Path $projectRoot $relative
    if(Test-Path -LiteralPath $path){$settings[$path] = [IO.File]::ReadAllBytes($path)}
}
$started = [DateTime]::UtcNow
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned Unity PID '+$process.Id+'; '+$runId)
try{$process.WaitForExit()}finally{foreach($path in $settings.Keys){[IO.File]::WriteAllBytes($path,$settings[$path])}}
$result = [ordered]@{run=$runId;mode=$Mode;startedUtc=$started.ToString('o');endedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$process.ExitCode;log=$log;status='unverified'}
if($Mode -ne 'Capture'){
    $receipt = Join-Path $projectRoot $(if($Mode -eq 'Install'){'Docs/VipStories/authoring_receipt.json'}else{'Docs/VipStories/refinement_receipt.json'})
    if((Test-Path -LiteralPath $receipt) -and (Get-Item -LiteralPath $receipt).LastWriteTimeUtc -ge $started){
        $result.receipt = $receipt
        $result.status = if($process.ExitCode -eq 0 -and (Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json).status -eq 'passed'){'passed'}else{'failed'}
    }
}else{
    $receipt = Join-Path $projectRoot 'Docs/VipStories/gameview_receipt.json'
    $layout = Join-Path $projectRoot 'Docs/VipStories/ui_overflow.json'
    if((Test-Path -LiteralPath $receipt) -and (Test-Path -LiteralPath $layout) -and (Get-Item -LiteralPath $receipt).LastWriteTimeUtc -ge $started -and (Get-Item -LiteralPath $layout).LastWriteTimeUtc -ge $started){
        $view = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
        $ui = Get-Content -LiteralPath $layout -Raw | ConvertFrom-Json
        $fresh = @($view.screenshots | Where-Object { !(Test-Path -LiteralPath $_) -or (Get-Item -LiteralPath $_).LastWriteTimeUtc -lt $started }).Count -eq 0
        $result.receipt = $receipt; $result.layout = $layout; $result.screenshots = $view.screenshots.Count
        $result.status = if($process.ExitCode -eq 0 -and $view.mode -eq 'actual_playmode_gameview' -and $view.screenshots.Count -eq 22 -and $fresh -and $ui.passed){'passed'}else{'failed'}
    }
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $reportRoot ($runId+'.json')) -Encoding utf8
$result | ConvertTo-Json -Depth 5
if($result.status -in @('failed','unverified')){exit 1}
