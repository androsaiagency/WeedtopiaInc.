[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$exe=Join-Path $projectRoot 'Builds/ProductionFinishDiagnostic/BurningHillsDiagnostic.exe'
if(-not(Test-Path -LiteralPath $exe)){throw 'Build the diagnostic player first.'}
$buildReceipt=Get-Content -LiteralPath (Join-Path $projectRoot 'Docs/ProductionFinish/Evidence/development_build.json') -Raw | ConvertFrom-Json
if(-not $buildReceipt.passed){throw 'The current diagnostic build did not pass.'}
$exeHash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
if(Get-Process BurningHillsDiagnostic -ErrorAction SilentlyContinue){throw 'An existing diagnostic player is open; it will not be interrupted.'}
$runId=Get-Date -Format 'yyyyMMdd_HHmmss'
$output=Join-Path $projectRoot ('Docs/ProductionFinish/Evidence/player_profile_'+$runId+'.json')
$log=Join-Path $projectRoot ('Docs/ProductionFinish/Evidence/player_'+$runId+'.log')
# Batch mode keeps the diagnostic simulation active without taking user focus.
# Keep graphics enabled: the benchmark submits real URP render requests and verifies draw calls.
$arguments=@('-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','--weedtopia-diagnostic',('"'+$output+'"'),'-logFile',('"'+$log+'"'))
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned diagnostic player PID '+$process.Id)
$process.WaitForExit()
if($process.ExitCode -ne 0 -or -not(Test-Path -LiteralPath $output) -or (Get-Item -LiteralPath $output).LastWriteTimeUtc -lt $started){throw ('Diagnostic player failed or lacks fresh receipt. See '+$log)}
$report=Get-Content -LiteralPath $output -Raw | ConvertFrom-Json
if(-not $report.completed -or $report.measurements.Count -ne 3){throw 'Player receipt is incomplete.'}
if(@($report.measurements | Where-Object { $_.maxDrawCalls -le 0 }).Count -gt 0){throw ('No rendered draw calls in a measured scenario. Reject timing evidence: '+$output)}
[ordered]@{utc=[DateTime]::UtcNow.ToString('o');exitCode=$process.ExitCode;profile=$output;log=$log;fingerprint=$buildReceipt.fingerprint;executableSha256=$exeHash;execution='batchmode with Direct3D11 graphics and explicit URP render requests';diagnosticCompleted=$true;releaseAccepted=$false} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectRoot 'Docs/ProductionFinish/Evidence/player_run.json') -Encoding utf8
Get-Content -LiteralPath $output
