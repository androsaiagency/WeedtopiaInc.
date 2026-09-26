[CmdletBinding()]
param(
    [ValidateSet('EditMode','PlayMode','Install','Prototype','Validate','Capture','Polish','Production','Preflight','ProductionCapture','ProductionAudio','ProductionRefine','ProductionBuild','Layout','BuildingSprites','LayoutCapture','LayoutRefine','Environment','EnvironmentRefine','EnvironmentCapture','Interaction','InteractionCapture','BuildingAlignment','BuildingAlignmentCapture','BakeryDelivery','BakeryCapture','LivingHills','LivingHillsRefine','LivingHillsCapture','DirectionalMotion','DirectionalMotionCapture','FarmGardenBefore','FarmGardenCapture','FarmGarden','FarmGardenMeshes')][string]$Mode = 'EditMode',
    [string]$Label = '',
    [string]$Assembly = '',
    [string]$TestFilter = '',
    [string]$UnityEditor = 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$reportRoot = Join-Path $projectRoot 'Docs/Implementation/Validation'
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))) {
    throw 'Existing Unity editor or lock. No second editor is allowed.'
}
$runId = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + $Mode + $(if($Label){'_'+$Label}else{''})
$log = Join-Path $reportRoot ($runId + '.log')
$xml = Join-Path $reportRoot ($runId + '.xml')
$arguments = @('-batchmode', '-projectPath', ('"'+$projectRoot+'"'), '-logFile', ('"'+$log+'"'))
if($Mode -notin @('Capture','ProductionCapture','LayoutCapture','EnvironmentCapture','InteractionCapture','BuildingAlignmentCapture','BakeryCapture','LivingHillsCapture','DirectionalMotionCapture','FarmGardenBefore','FarmGardenCapture')) { $arguments += '-nographics' }
if($Mode -in @('EditMode','PlayMode')) {
    $arguments += @('-runTests','-testPlatform',$Mode,'-testResults',('"'+$xml+'"'))
    if($Assembly) { $arguments += @('-assemblyNames',$Assembly) }
    if($TestFilter) { $arguments += @('-testFilter',$TestFilter) }
} else {
    $method = switch($Mode) {
        'Prototype' {'Weedtopia.Editor.BuildingArtPipeline.BuildingSpritePrototypeUtility.CreateTownHallPrototypeMenu'}
        'Install' {'Weedtopia.Editor.GameSetup.GameScaffold.CreateOrRepair'}
        'Validate' {'Weedtopia.Editor.GameSetup.GameScaffold.ValidateBatch'}
        'Capture' {'Weedtopia.Editor.GameSetup.VisualEvidence.Begin'}
        'Polish' {'Weedtopia.Editor.GameSetup.GamePolish.Apply'}
        'ProductionRefine' {'Weedtopia.Editor.GameSetup.ProductionRefinement.Apply'}
        'ProductionAudio' {'Weedtopia.Editor.GameSetup.ProductionAudioAuthoring.Bind'}
        'Production' {'Weedtopia.Editor.GameSetup.ProductionFinishInstaller.Apply'}
        'Preflight' {'Weedtopia.Editor.GameSetup.ProductionVisualPreflight.RunMenu'}
        'ProductionCapture' {'Weedtopia.Editor.GameSetup.ProductionVisualEvidence.Begin'}
        'ProductionBuild' {'Weedtopia.Editor.GameSetup.ProductionDiagnosticBuild.Build'}
        'Layout' {'Weedtopia.Editor.GameSetup.BurningHillsLayoutInstaller.Apply'}
        'BuildingSprites' {'Weedtopia.Editor.GameSetup.ProductionBuildingIntegration.Apply'}
        'LayoutCapture' {'Weedtopia.Editor.GameSetup.LayoutVisualEvidence.Begin'}
        'LayoutRefine' {'Weedtopia.Editor.GameSetup.DistrictLandscapeRefinement.Apply'}
        'Environment' {'Weedtopia.Editor.GameSetup.NaturalEnvironmentAuthoring.Apply'}
        'EnvironmentRefine' {'Weedtopia.Editor.GameSetup.NaturalEnvironmentAuthoring.RebuildOwned'}
        'EnvironmentCapture' {'Weedtopia.Editor.GameSetup.LayoutVisualEvidence.BeginEnvironment'}
        'Interaction' {'Weedtopia.Editor.GameSetup.BuildingInteractionAuthoring.Apply'}
        'InteractionCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.Begin'}
        'BuildingAlignment' {'Weedtopia.Editor.GameSetup.ProductionBuildingIntegration.CenterFootprints'}
        'BuildingAlignmentCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginAlignment'}
        'BakeryDelivery' {'Weedtopia.Editor.GameSetup.BakeryDeliveryAuthoring.Apply'}
        'BakeryCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginBakery'}
        'LivingHills' {'Weedtopia.Editor.GameSetup.LivingHillsAuthoring.Apply'}
        'LivingHillsRefine' {'Weedtopia.Editor.GameSetup.LivingHillsAuthoring.RebuildOwned'}
        'LivingHillsCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginLivingHills'}
        'DirectionalMotion' {'Weedtopia.Editor.GameSetup.DirectionalMotionAuthoring.Apply'}
        'DirectionalMotionCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginDirectionalMotion'}
        'FarmGardenBefore' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginFarmGardenBefore'}
        'FarmGardenCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginFarmGardenAfter'}
        'FarmGarden' {'Weedtopia.Editor.GameSetup.FarmGardenRefinement.Apply'}
        'FarmGardenMeshes' {'Weedtopia.Editor.GameSetup.FarmGardenRefinement.RefreshPathMeshes'}
    }
    $arguments += @('-executeMethod',$method)
    if($Mode -notin @('Capture','ProductionCapture','LayoutCapture','EnvironmentCapture','InteractionCapture','BuildingAlignmentCapture','BakeryCapture','LivingHillsCapture','DirectionalMotionCapture','FarmGardenBefore','FarmGardenCapture')) { $arguments += '-quit' }
}
$start = [DateTime]::UtcNow
# Unity/URP can serialize build caches and enable Unity Connect during a diagnostic build.
# Preserve pre-existing user settings byte-for-byte; authored game assets stay under the explicit installers.
$preservedSettings = @{}
$settingsPaths = @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')
foreach($relative in $settingsPaths) {
    $settingsPath=Join-Path $projectRoot $relative
    if(Test-Path -LiteralPath $settingsPath){$preservedSettings[$settingsPath]=[IO.File]::ReadAllBytes($settingsPath)}
}
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned Unity PID '+$process.Id+'; '+$runId)
$process.WaitForExit()
foreach($settingsPath in $preservedSettings.Keys){[IO.File]::WriteAllBytes($settingsPath,$preservedSettings[$settingsPath])}
$result = [ordered]@{run=$runId;mode=$Mode;startedUtc=$start.ToString('o');endedUtc=[DateTime]::UtcNow.ToString('o');unity='6000.3.9f1 (7a9955a4f2fa)';exitCode=$process.ExitCode;log=$log;status='unverified'}
if($Mode -in @('EditMode','PlayMode')) {
    if((Test-Path $xml) -and (Get-Item $xml).LastWriteTimeUtc -ge $start) {
        [xml]$doc=Get-Content $xml -Raw
        $result.total=[int]$doc.'test-run'.total; $result.passed=[int]$doc.'test-run'.passed
        $result.failed=[int]$doc.'test-run'.failed; $result.skipped=[int]$doc.'test-run'.skipped; $result.xml=$xml
        if($process.ExitCode -eq 0 -and $result.total -gt 0 -and $doc.'test-run'.result -eq 'Passed'){$result.status='passed'}else{$result.status='failed'}
    }
} else { $result.status = if($process.ExitCode -eq 0){'process_completed_inspect_receipt'}else{'failed'} }
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $reportRoot ($runId+'.json')) -Encoding utf8
$result | ConvertTo-Json -Depth 5
if($result.status -in @('failed','unverified')){exit 1}
