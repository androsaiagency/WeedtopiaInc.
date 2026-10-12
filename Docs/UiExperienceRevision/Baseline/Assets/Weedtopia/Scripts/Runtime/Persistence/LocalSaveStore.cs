using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Persistence
{
    [Serializable] public sealed class SaveEnvelope { public int format = 1; public string payload, sha256; }
    public sealed class LocalSaveStore : ISaveStore
    {
        public string Path { get; }
        private readonly GameDefinitions definitions;
        private readonly object sync = new object();
        public bool Exists => File.Exists(Path) || File.Exists(Path + ".bak");
        public LocalSaveStore(string path, GameDefinitions definitions) { Path = System.IO.Path.GetFullPath(path); this.definitions = definitions; }
        public void Save(GameState state)
        {
            SaveValidator.Validate(state, definitions);
            string payload = JsonUtility.ToJson(state);
            string json = JsonUtility.ToJson(new SaveEnvelope { payload = payload, sha256 = Hash(payload) });
            lock(sync)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                string temporary = Path + ".tmp";
                using(var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(json); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                // Validate the exact bytes on disk before replacing the live save.
                Decode(File.ReadAllText(temporary));
                if (File.Exists(Path))
                {
                    bool valid;
                    try { Decode(File.ReadAllText(Path)); valid = true; }
                    catch (Exception ex) when (ex is ArgumentException || ex is InvalidDataException || ex is InvalidOperationException) { valid = false; }
                    if (!valid) File.Copy(Path, Path + ".corrupt_" + Guid.NewGuid().ToString("N"));
                    File.Replace(temporary, Path, valid ? Path + ".bak" : null);
                }
                else File.Move(temporary, Path);
            }
        }
        public SaveLoadResult Load()
        {
            lock(sync)
            {
                string error = null;
                foreach (string path in new[] { Path, Path + ".bak" })
                {
                    if (!File.Exists(path)) continue;
                    try { return new SaveLoadResult { State = Decode(File.ReadAllText(path)), Recovered = path != Path, Message = path != Path ? "ui.recovered" : null }; }
                    catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is InvalidOperationException) { error = ex.Message; }
                }
                return new SaveLoadResult { Message = error == null ? "ui.no_save" : "save.invalid" };
            }
        }
        public GameState Decode(string json)
        {
            var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            if (envelope == null || envelope.format != 1 || string.IsNullOrEmpty(envelope.payload) || Hash(envelope.payload) != envelope.sha256) throw new InvalidDataException("Save checksum/format mismatch");
            var state = JsonUtility.FromJson<GameState>(envelope.payload);
            if (state == null || state.schemaVersion < 1 || state.schemaVersion > 2) throw new InvalidDataException("Unsupported save schema");
            VipStorySaveValidator.RestoreSerializedNulls(state);
            if (state.schemaVersion == 1)
            {
                state.highWaterUtcTicks = Math.Max(state.highWaterUtcTicks, state.lastUtcTicks);
                state.schemaVersion = 2;
                foreach (var b in state.buildings) if (b.dailyTier == 0) b.dailyTier = VisualTiers.Index(b.level);
            }
            MigrateContent(state);
            SaveValidator.Validate(state, definitions); return state;
        }
        private void MigrateContent(GameState state)
        {
            if (state.contentVersion >= definitions.version) return;
            if (state.contentVersion < 1 || definitions.version > 3) throw new InvalidDataException("Unsupported content migration");
            // Validate the historical catalog before adding anything. A missing old building is corruption.
            var previous = JsonUtility.FromJson<GameDefinitions>(JsonUtility.ToJson(definitions));
            previous.version = state.contentVersion;
            previous.instances = previous.instances.Where(x => x.introducedVersion <= previous.version).ToArray();
            var ids = previous.instances.Select(x => x.id).ToHashSet();
            var cityIds = previous.instances.Select(x => x.cityId).ToHashSet();
            previous.routes = previous.routes.Where(x => ids.Contains(x.from) && ids.Contains(x.to)).ToArray();
            previous.cities = (previous.cities ?? Array.Empty<CityDefinition>()).Where(x => x.introducedVersion <= previous.version).ToArray();
            previous.quests = previous.quests.Where(x => cityIds.Contains(string.IsNullOrEmpty(x.cityId) ? "burning_hills" : x.cityId)).ToArray();
            previous.vipStories = (previous.vipStories ?? Array.Empty<VipStoryDefinition>()).Where(x => cityIds.Contains(x.cityId)).ToArray();
            previous.vipCollaborations = (previous.vipCollaborations ?? Array.Empty<VipCollaborationDefinition>()).Where(x => cityIds.Contains(x.cityId)).ToArray();
            SaveValidator.Validate(state, previous);
            foreach (string cityId in definitions.instances.Select(x => x.cityId).Distinct().Where(id => !cityIds.Contains(id)))
                state.cities.Add(new CityState { id = cityId });
            foreach (var instance in definitions.instances.Where(x => x.introducedVersion > state.contentVersion))
                state.buildings.Add(new BuildingState { id = instance.id, family = instance.family,
                    cityId = instance.cityId, level = definitions.Building(instance.family).kind == BuildingKind.TownHall ? 1 : 0,
                    lastDailyDay = state.highWaterUtcTicks / TimeSpan.TicksPerDay - 1 });
            state.contentVersion = definitions.version;
        }
        public static string Hash(string text)
        {
            using(var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
    }

    public static class SaveValidator
    {
        public static void Validate(GameState s, GameDefinitions d)
        {
            void Require(bool valid, string message) { if(!valid) throw new InvalidDataException(message); }
            bool AmountValid(Amount a) => a.IsValid && a >= 0;
            Require(s != null, "Missing save");
            Require(s.schemaVersion == 2 && s.contentVersion == d.version, "Schema/content version mismatch");
            Require(s.playerName != null && s.playerName.Trim().Length >= 2 && s.playerName.Length <= 24 && !s.playerName.Any(x => char.IsControl(x) || x == '<' || x == '>'), "Invalid name");
            Require(AmountValid(s.money) && s.grinders >= 0 && s.cards >= 0 && s.randomState != 0, "Invalid resources/random state");
            Require(s.lastUtcTicks >= 0 && s.lastUtcTicks <= DateTime.MaxValue.Ticks && s.highWaterUtcTicks >= s.lastUtcTicks && s.highWaterUtcTicks <= DateTime.MaxValue.Ticks, "Invalid timestamps");
            Require(s.simulationSeconds >= 0 && s.tickRemainder >= 0 && s.tickRemainder < 1 && !double.IsNaN(s.tickRemainder), "Invalid simulation time");
            Require(s.volume >= 0 && s.volume <= 1 && s.uiScale >= .75f && s.uiScale <= 1.5f, "Invalid settings");
            Require(s.cities != null && s.buildings != null && s.workers != null && s.deliveries != null && s.vip != null, "Missing state collections");
            Require(s.cities.All(x=>x!=null)&&s.buildings.All(x=>x!=null)&&s.workers.All(x=>x!=null)&&s.deliveries.All(x=>x!=null)&&s.vip.All(x=>x!=null),"Null state entry");
            Require(s.cities.Select(x => x.id).Distinct().Count() == s.cities.Count && s.cities.Any(x => x.id == s.activeCity), "Invalid cities");
            Require(s.cities.Select(x => x.id).OrderBy(x => x).SequenceEqual(d.instances.Select(x => x.cityId).Distinct().OrderBy(x => x)), "City catalog mismatch");
            Require(CityCampaignRules.Accessible(d, s, s.activeCity), "Active city is locked");
            Require(s.buildings.Count == d.instances.Length && s.buildings.Select(x => x.id).Distinct().Count() == s.buildings.Count, "Invalid instance count/IDs");
            foreach(var b in s.buildings)
            {
                var instance = d.instances.FirstOrDefault(x => x.id == b.id);
                Require(instance != null && instance.cityId == b.cityId && s.cities.Any(x => x.id == b.cityId), "Invalid building identity");
                Require(b.family == instance.family || (instance.family == "settlement_plot" && (b.family == "park" || b.family == "residential_house")), "Invalid plot family");
                var definition = d.Building(b.family);
                Require(b.level >= 0 && b.level <= 5000 && b.buildingSeconds >= 0 && b.buildingSeconds <= definition.maxBuildSeconds && (b.level < 5000 || b.buildingSeconds == 0), "Invalid construction");
                Require(b.cycleProgress >= 0 && b.workSeconds >= 0 && b.workSeconds <= 30 && b.routeCursor >= 0 && b.dailyTier >= 0 && b.dailyTier <= 24, "Invalid building timing");
                Require(AmountValid(b.raw) && AmountValid(b.refined) && AmountValid(b.butter) && AmountValid(b.pastry), "Invalid buffer");
            }
            Require(s.workers.Select(x => x.id).Distinct().Count() == s.workers.Count, "Duplicate workers");
            Require(s.workers.Where(x => !string.IsNullOrEmpty(x.buildingId)).Select(x => x.buildingId+"/"+x.slot).Distinct().Count() == s.workers.Count(x => !string.IsNullOrEmpty(x.buildingId)), "Duplicate assignments");
            var workers = new WorkerService(d, s);
            foreach(var w in s.workers)
            {
                var role = d.roles.FirstOrDefault(x => x.id == w.role);
                Require(role != null && w.rarity >= 0 && w.rarity <= 4 && w.level >= 1 && w.level <= 100 && w.rank >= 1 && w.rank <= 10, "Invalid worker");
                Require(w.attributes != null && w.attributes.Count == w.rarity+1 && w.attributes.Distinct().Count() == w.attributes.Count && w.attributes.All(x => role.attributes.Contains(x)), "Invalid attributes");
                if (!string.IsNullOrEmpty(w.buildingId))
                {
                    var b = s.buildings.FirstOrDefault(x => x.id == w.buildingId);
                    Require(b != null && b.level > 0 && w.slot >= 0 && w.slot <= workers.Slots(b) && workers.SlotRole(b,w.slot) == w.role, "Invalid assignment");
                }
            }
            Require(s.deliveries.Select(x => x.workerId).Distinct().Count() == s.deliveries.Count, "Duplicate vehicle");
            foreach(var delivery in s.deliveries)
            {
                var route = d.routes.FirstOrDefault(x => x.id == delivery.route);
                Require(route != null && route.vehicle && s.workers.Any(x => x.id == delivery.workerId &&
                    (route.pickupByRecipient ? x.buildingId == route.to && x.slot == 0 : x.buildingId == route.from && x.slot > 0)), "Invalid delivery references");
                Require(AmountValid(delivery.cargo) && AmountValid(delivery.reservedCargo) && delivery.remainingSeconds >= 0 && delivery.totalSeconds >= 1 &&
                    delivery.totalSeconds >= delivery.remainingSeconds && delivery.totalSeconds <= route.travelSeconds, "Invalid delivery data");
                Require(delivery.fetching ? route.pickupByRecipient && delivery.cargo == 0 && delivery.reservedCargo > 0 :
                    delivery.reservedCargo == 0 && (route.pickupByRecipient || delivery.cargo > 0), "Invalid delivery phase");
            }
            foreach(var city in s.cities)
            {
                var quests = d.QuestsFor(city.id);
                Require(city.questIndex >= 0 && city.questIndex <= quests.Length && city.completedQuests != null && city.talked != null && city.stats != null && city.unlocked >= Zone.Farm && city.unlocked <= Zone.Industry, "Invalid progression");
                Require(city.completedQuests.SequenceEqual(quests.Take(city.questIndex).Select(x=>x.id)) && city.freeplay == (city.questIndex == quests.Length), "Quest history mismatch");
                var destination = (d.cities ?? Array.Empty<CityDefinition>()).FirstOrDefault(c => c.id == city.id);
                if (destination != null && !string.IsNullOrEmpty(destination.sourceCity) && city.charterAccepted)
                    Require(s.cities.Any(c => c.id == destination.sourceCity && c.storyResidentMilestone >= destination.unlockResidents), "Invalid city charter milestone");
                if (!CityCampaignRules.Accessible(d, s, city.id))
                    Require(city.questIndex == 0 && city.unlocked == Zone.Farm && city.storyResidentMilestone == 0 &&
                        (city.vipStories == null || city.vipStories.Count == 0) && city.stats.playSeconds == 0, "Progress in locked city");
                var st=city.stats;
                Require(new[]{st.earned,st.rawProduced,st.refinedProduced,st.butterProduced,st.pastryProduced,st.coffeeSold,st.pastrySold,st.delivered}.All(AmountValid),"Invalid statistics");
                ValidateProject(city, d);
            }
            RewardSaveValidator.Validate(s, d);
            VipStorySaveValidator.Validate(s, d);
            Require(s.vip.Select(x=>x.id).Distinct().Count()==s.vip.Count,"Duplicate VIP quests");
            foreach(var vip in s.vip) Require(s.cities.Any(x=>x.id==vip.cityId) && AmountValid(vip.target) && AmountValid(vip.reward) && (vip.kind==ConditionKind.Delivered||vip.kind==ConditionKind.Harvest),"Invalid VIP");
        }

        private static void ValidateProject(CityState city, GameDefinitions defs)
        {
            void Require(bool valid) { if (!valid) throw new InvalidDataException("Invalid city project progress"); }
            var progress = city.project;
            // Additive optional save field: old saves have no chosen plan and keep all existing progress.
            if (progress == null) return;
            if (string.IsNullOrEmpty(progress.planId))
            {
                Require(progress.stepIndex == 0 && (progress.completedSteps == null || progress.completedSteps.Count == 0) &&
                    (progress.baselines == null || progress.baselines.Length == 0));
                return;
            }
            var definition = defs.cityProject;
            Require(definition != null && definition.cityId == city.id && city.freeplay);
            var plan = definition.plans.FirstOrDefault(p => p.id == progress.planId);
            Require(plan != null && progress.stepIndex >= 0 && progress.stepIndex <= plan.steps.Length);
            Require(progress.completedSteps != null && progress.completedSteps.SequenceEqual(plan.steps.Take(progress.stepIndex).Select(s => s.id)));
            var goals = progress.stepIndex < plan.steps.Length ? plan.steps[progress.stepIndex].goals : Array.Empty<ProjectGoal>();
            Require(progress.baselines != null && progress.baselines.Length == goals.Length);
            for (int i = 0; i < goals.Length; i++)
            {
                Amount baseline = progress.baselines[i];
                Require(baseline.IsValid && baseline >= 0);
                var stats = city.stats;
                Amount current;
                switch (goals[i].kind)
                {
                    case ProjectGoalKind.RefinedProduced: current = stats.refinedProduced; break;
                    case ProjectGoalKind.CoffeeSold: current = stats.coffeeSold; break;
                    case ProjectGoalKind.ButterProduced: current = stats.butterProduced; break;
                    case ProjectGoalKind.PastrySold: current = stats.pastrySold; break;
                    case ProjectGoalKind.Delivered: current = stats.delivered; break;
                    default: current = 0; break;
                }
                Require(baseline <= current);
            }
        }
    }
}
