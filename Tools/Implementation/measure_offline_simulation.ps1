param([int]$Seconds = 28800)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$taskDomain = Join-Path $taskRoot 'Docs/Implementation/Validation/SourceCompile/Weedtopia.Domain.dll'
[void][Reflection.Assembly]::LoadFrom($taskDomain)
$taskOptions = [System.Text.Json.JsonSerializerOptions]::new()
$taskOptions.IncludeFields = $true
$taskOptions.IgnoreReadOnlyProperties = $true
$taskDefs = [System.Text.Json.JsonSerializer]::Deserialize([IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/Weedtopia/Game/Content/BurningHills.json')), [Weedtopia.Domain.GameDefinitions], $taskOptions)
$taskReference = [IO.File]::ReadAllText((Join-Path $taskRoot 'Docs/CommercialReadiness/2026-10-05/Baseline/Simulation.cs')).Replace('class Simulation', 'class ReferenceSimulation').Replace('public Simulation(', 'public ReferenceSimulation(')
$taskHarness = @'
namespace Weedtopia.Domain {
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Weedtopia.Domain;
public static class OfflineBenchmark
{
    public static GameState Prepare(GameDefinitions defs, bool established)
    {
        var state = new GameState { playerName = "Benchmark", introSeen = true, tickRemainder = .375,
            highWaterUtcTicks = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc).Ticks };
        foreach (string id in defs.instances.Select(i => i.cityId).Distinct())
            state.cities.Add(new CityState { id = id, charterAccepted = established, unlocked = established ? Zone.Industry : Zone.Farm });
        int index = 0;
        foreach (var instance in defs.instances)
        {
            var definition = defs.Building(instance.family);
            var b = new BuildingState { id = instance.id, family = instance.family, cityId = instance.cityId,
                level = established ? 500 : definition.kind == BuildingKind.TownHall ? 1 : 0 };
            if (established && definition.kind == BuildingKind.Plot)
            { b.family = index++ % 2 == 0 ? "residential_house" : "park"; definition = defs.Building(b.family); }
            if (established)
            {
                b.buildingSeconds = index++ % 7 == 0 ? 12 : 0;
                b.workSeconds = 3;
                b.raw = 50; b.refined = 20; b.butter = 10; b.pastry = 5;
                if (!string.IsNullOrEmpty(definition.managerRole)) AddWorker(state, b, definition.managerRole, 0);
                if (!string.IsNullOrEmpty(definition.driverRole))
                    for (int slot = 1; slot <= 2; slot++) AddWorker(state, b, definition.driverRole, slot);
            }
            state.buildings.Add(b);
        }
        return state;
    }
    private static void AddWorker(GameState state, BuildingState b, string role, int slot)
    {
        var worker = new WorkerState { id = "bench_" + state.workers.Count, role = role, buildingId = b.id, slot = slot,
            level = 4, rank = 2, rarity = 4 };
        worker.attributes.AddRange((WorkerAttribute[])Enum.GetValues(typeof(WorkerAttribute)));
        state.workers.Add(worker);
    }
    public static double[] Run(GameDefinitions defs, GameState state, bool reference, int seconds)
    {
        Action<double,long?> advance;
        if (reference) advance = new ReferenceSimulation(defs, state).Advance;
        else advance = new Simulation(defs, state).Advance;
        advance(1, state.highWaterUtcTicks);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        advance(seconds, state.highWaterUtcTicks + TimeSpan.TicksPerSecond);
        watch.Stop();
        return new[] { watch.Elapsed.TotalMilliseconds, (double)(GC.GetAllocatedBytesForCurrentThread() - before) };
    }
}
}
'@
$taskRefs = @($taskDomain) + @(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
Add-Type -TypeDefinition ($taskReference + $taskHarness) -ReferencedAssemblies $taskRefs
$taskRows = @()
foreach ($taskScenario in @('early-save', 'established-two-cities', 'vip-expiry')) {
    $taskEstablished = $taskScenario -ne 'early-save'
    $taskOld = [Weedtopia.Domain.OfflineBenchmark]::Prepare($taskDefs, $taskEstablished)
    $taskNew = [Weedtopia.Domain.OfflineBenchmark]::Prepare($taskDefs, $taskEstablished)
    if ($taskScenario -eq 'vip-expiry') {
        foreach ($taskState in @($taskOld, $taskNew)) {
            $taskState.shop = [Weedtopia.Domain.ShopState]::new()
            $taskState.shop.vipMembership = [Weedtopia.Domain.VipMembershipState]::new()
            $taskState.shop.vipMembership.startedUtcTicks = $taskState.highWaterUtcTicks + 20 * [TimeSpan]::TicksPerSecond
            $taskState.shop.vipMembership.expiresUtcTicks = $taskState.highWaterUtcTicks + 150 * [TimeSpan]::TicksPerSecond
        }
    }
    $taskOldRun = [Weedtopia.Domain.OfflineBenchmark]::Run($taskDefs, $taskOld, $true, $Seconds)
    $taskNewRun = [Weedtopia.Domain.OfflineBenchmark]::Run($taskDefs, $taskNew, $false, $Seconds)
    $taskOldJson = [System.Text.Json.JsonSerializer]::Serialize($taskOld, [Weedtopia.Domain.GameState], $taskOptions)
    $taskNewJson = [System.Text.Json.JsonSerializer]::Serialize($taskNew, [Weedtopia.Domain.GameState], $taskOptions)
    $taskRow = [ordered]@{ scenario = $taskScenario; seconds = $Seconds;
        buildings = $taskNew.buildings.Count; workers = $taskNew.workers.Count; originalMilliseconds = $taskOldRun[0]; optimizedMilliseconds = $taskNewRun[0];
        speedup = $taskOldRun[0] / $taskNewRun[0]; originalAllocatedBytes = $taskOldRun[1]; optimizedAllocatedBytes = $taskNewRun[1];
        exactStateMatch = $taskOldJson -ceq $taskNewJson }
    $taskRows += $taskRow
    $taskRow | ConvertTo-Json -Compress
    if (-not $taskRow.exactStateMatch) {
        [IO.File]::WriteAllText((Join-Path $taskRoot '.utmp/offline-original.json'), $taskOldJson)
        [IO.File]::WriteAllText((Join-Path $taskRoot '.utmp/offline-optimized.json'), $taskNewJson)
        throw 'Offline state differs from the original simulation.'
    }
}
$taskReport = [ordered]@{ utc = [DateTime]::UtcNow.ToString('O'); gate = 'standalone-domain-benchmark'; runtime = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription;
    sourceSha256 = (Get-FileHash -LiteralPath (Join-Path $taskRoot 'Assets/Weedtopia/Scripts/Runtime/Domain/Simulation.cs')).Hash;
    referenceSha256 = (Get-FileHash -LiteralPath (Join-Path $taskRoot 'Docs/CommercialReadiness/2026-10-05/Baseline/Simulation.cs')).Hash; scenarios = $taskRows }
$taskOut = Join-Path $taskRoot ('Docs/CommercialReadiness/2026-10-05/offline-benchmark-' + $Seconds + '.json')
[IO.File]::WriteAllText($taskOut, ($taskReport | ConvertTo-Json -Depth 8))
