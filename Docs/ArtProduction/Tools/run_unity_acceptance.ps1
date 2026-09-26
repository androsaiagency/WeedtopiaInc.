[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('EditMode', 'FinalValidate')]
    [string]$Mode,
    [string]$UnityEditor = 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe'
)

# Never delete a Unity lock, stop an editor, or start a second editor here.
# Run outside a restrictive file sandbox so Unity can use its normal local cache.
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$reportRoot = Join-Path $projectRoot 'Docs/ArtProduction/Validation'
$lockPath = Join-Path $projectRoot 'Temp/UnityLockfile'

function Assert-EditorAvailable {
    if (Get-Process -Name Unity -ErrorAction SilentlyContinue) {
        throw 'An editor is already running. This script will not start another one.'
    }
    if (Test-Path -LiteralPath $lockPath) {
        throw 'Temp/UnityLockfile exists. This script will not manipulate it or start an editor.'
    }
}

Assert-EditorAvailable
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw ('Unity editor is missing: ' + $UnityEditor)
}

$logPath = Join-Path $reportRoot ('unity_' + $Mode.ToLowerInvariant() + '.log')
$resultPath = Join-Path $reportRoot 'unity_editmode_results.xml'
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $projectRoot + '"'), '-logFile', ('"' + $logPath + '"'))
if ($Mode -eq 'FinalValidate') {
    # Avoid starting a final Unity check with known missing art artifacts.
    $preflightRelativeReport = 'Docs/ArtProduction/Validation/unity_preflight_production_validation.json'
    $preflightReport = Join-Path $projectRoot $preflightRelativeReport
    & python (Join-Path $PSScriptRoot 'validate_pipeline.py') --root $projectRoot validate --report $preflightRelativeReport
    if ($LASTEXITCODE -ne 0) {
        throw ('Production completeness has not passed. No Unity process started. Inspect ' + $preflightReport)
    }
    $arguments += @('-quit', '-executeMethod', 'Weedtopia.Editor.BuildingArtPipeline.BuildingIntakeBatch.ValidateAndWriteReport')
}
else {
    # Test Runner controls its own exit; -quit would interrupt the test run.
    $arguments += @('-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'Weedtopia.Editor.BuildingArtPipeline.Tests', '-testResults', ('"' + $resultPath + '"'))
}

Assert-EditorAvailable
$startedUtc = [DateTime]::UtcNow
$unityProcess = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned Unity ' + $Mode + ' PID: ' + $unityProcess.Id)
$unityProcess.WaitForExit()
$unityExitCode = $unityProcess.ExitCode
$status = [ordered]@{
    started_utc = $startedUtc.ToString('o')
    ended_utc = [DateTime]::UtcNow.ToString('o')
    mode = $Mode
    exit_code = $unityExitCode
    log = $logPath
    lock_present_after_exit = Test-Path -LiteralPath $lockPath
    result = 'failed'
}

if ($Mode -eq 'EditMode' -and (Test-Path -LiteralPath $resultPath)) {
    $resultFile = Get-Item -LiteralPath $resultPath
    if ($resultFile.LastWriteTimeUtc -ge $startedUtc) {
        [xml]$testResult = Get-Content -LiteralPath $resultPath -Raw
        $status.results_xml = $resultPath
        $status.tests = [int]$testResult.'test-run'.total
        $status.passed = [int]$testResult.'test-run'.passed
        $status.failed = [int]$testResult.'test-run'.failed
        $status.skipped = [int]$testResult.'test-run'.skipped
        if ($unityExitCode -eq 0 -and $testResult.'test-run'.result -eq 'Passed' -and $status.tests -gt 0 -and $status.failed -eq 0) {
            $status.result = 'passed'
        }
    }
}
elseif ($Mode -eq 'FinalValidate' -and $unityExitCode -eq 0) {
    $intakeReportPath = Join-Path $reportRoot 'unity_intake_report.json'
    if ((Test-Path -LiteralPath $intakeReportPath) -and (Get-Item -LiteralPath $intakeReportPath).LastWriteTimeUtc -ge $startedUtc) {
        $intakeReport = Get-Content -LiteralPath $intakeReportPath -Raw | ConvertFrom-Json
        $status.intake_report = $intakeReportPath
        $status.errors = $intakeReport.errors
        $status.warnings = $intakeReport.warnings
        $status.wrappers = $intakeReport.wrapperSlots
        $status.catalog_references = $intakeReport.catalogSlots
        $status.reference_images = $intakeReport.referenceImages
        if ($intakeReport.errors -eq 0 -and $intakeReport.wrapperSlots -eq 251 -and $intakeReport.catalogSlots -eq 251 -and $intakeReport.referenceImages -eq 251) {
            $status.result = 'passed'
        }
    }
}

$statusPath = Join-Path $reportRoot ('unity_' + $Mode.ToLowerInvariant() + '_status.json')
$status | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statusPath -Encoding utf8
$status | ConvertTo-Json -Depth 5 | Write-Output
if ($status.result -ne 'passed') { exit 1 }
exit 0
