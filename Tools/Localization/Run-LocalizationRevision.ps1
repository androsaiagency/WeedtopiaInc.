[CmdletBinding()]
param([string]$UnityEditor = 'D:\Unity Games\6000.3.9f1\Editor\Unity.exe')

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$reportRoot = Join-Path $projectRoot 'Docs/LocalizationRevision/2026-10-06'
if ((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))) {
    throw 'Existing Unity editor or lock. Revision evidence must never start a second editor.'
}
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
$log = Join-Path $reportRoot 'Capture.log'
$arguments = @('-batchmode', '-projectPath', ('"' + $projectRoot + '"'), '-logFile', ('"' + $log + '"'),
    '-executeMethod', 'Weedtopia.Editor.GameSetup.LocalizationEvidence.BeginRevision')
$preserved = @{}
foreach ($relative in @('ProjectSettings/ProjectSettings.asset', 'ProjectSettings/UnityConnectSettings.asset',
        'Assets/Settings/PC_RPAsset.asset', 'Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')) {
    $target = Join-Path $projectRoot $relative
    if (Test-Path -LiteralPath $target) { $preserved[$target] = [IO.File]::ReadAllBytes($target) }
}
$started = [DateTime]::UtcNow
$owned = $null
try {
    $owned = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Write-Output ('Owned localization revision Unity PID ' + $owned.Id)
    $owned.WaitForExit()
} finally {
    foreach ($target in $preserved.Keys) { [IO.File]::WriteAllBytes($target, $preserved[$target]) }
}
$receipt = Join-Path $reportRoot 'gameview_receipt.json'
$status = 'unverified'
if ($owned -and $owned.ExitCode -eq 0 -and (Test-Path -LiteralPath $receipt) -and (Get-Item -LiteralPath $receipt).LastWriteTimeUtc -ge $started) {
    $details = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    if ($details.status -eq 'passed' -and $details.screenshots.Count -eq 31 -and $details.assertions -gt 100) { $status = 'passed' }
}
$result = [ordered]@{ status = $status; startedUtc = $started.ToString('o'); endedUtc = [DateTime]::UtcNow.ToString('o');
    ownedUnityProcessId = $owned.Id; exitCode = $owned.ExitCode; log = $log; receipt = $receipt; settingsRestored = $true }
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $reportRoot 'capture_run.json') -Encoding utf8
$result | ConvertTo-Json -Depth 4
if ($status -ne 'passed') { throw 'Localization revision evidence failed or is incomplete. Inspect Capture.log and gameview_receipt.json.' }
