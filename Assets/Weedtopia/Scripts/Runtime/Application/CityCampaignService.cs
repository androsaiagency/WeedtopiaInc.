using System;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    /// <summary>Optional government mandates run beside each city's own main quests and resident stories.</summary>
    public sealed class CityCampaignService
    {
        private readonly GameDefinitions defs;
        private readonly GameState state;
        private readonly Simulation simulation;
        public CityCampaignService(GameDefinitions definitions, GameState game, Simulation sim)
        { defs = definitions; state = game; simulation = sim; }
        public bool Accessible(string city) => CityCampaignRules.Accessible(defs, state, city);
        public bool AnyOffered => (defs.cities ?? Array.Empty<CityDefinition>()).Any(c => Offered(c.id));
        public long UnlockProgress(string id)
        {
            var definition = (defs.cities ?? Array.Empty<CityDefinition>()).FirstOrDefault(c => c.id == id);
            if (definition == null || !CityCampaignRules.HasCharter(definition)) return 0;
            if (CityCampaignRules.Combined(definition))
                return CityCampaignRules.Sources(definition).Sum(source => Accessible(source) ? (long)simulation.Residents(source) : 0);
            var city = state.cities.FirstOrDefault(c => c.id == definition.sourceCity);
            return city == null ? 0 : Math.Max(city.storyResidentMilestone, simulation.Residents(city.id));
        }
        public bool CanAccept(string id)
        {
            var definition = (defs.cities ?? Array.Empty<CityDefinition>()).FirstOrDefault(c => c.id == id);
            return definition != null && Offered(id) &&
                (CityCampaignRules.Combined(definition) || state.activeCity == definition.sourceCity);
        }
        public bool Offered(string id)
        {
            var city = (defs.cities ?? Array.Empty<CityDefinition>()).FirstOrDefault(c => c.id == id);
            if (city == null || !CityCampaignRules.HasCharter(city) || Accessible(id) || !state.introSeen) return false;
            if (!CityCampaignRules.Combined(city) && !CityCampaignRules.Sources(city).All(Accessible)) return false;
            var destination = state.cities.FirstOrDefault(c => c.id == id);
            return destination != null && (UnlockProgress(id) >= city.unlockResidents ||
                (CityCampaignRules.Combined(city) && destination.charterResidentMilestone >= city.unlockResidents));
        }
        public void RefreshMilestones()
        {
            if (!state.introSeen) return;
            foreach (var city in state.cities.Where(c => Accessible(c.id)))
            {
                int residents = simulation.Residents(city.id);
                if (residents <= city.storyResidentMilestone) continue;
                city.storyResidentMilestone = residents; state.revision++;
            }
            // Capture the largest total actually present at one moment. Independent historical city maxima must never unlock a combined mandate.
            foreach (var definition in (defs.cities ?? Array.Empty<CityDefinition>()).Where(CityCampaignRules.Combined))
            {
                var city = state.cities.FirstOrDefault(c => c.id == definition.id);
                long residents = UnlockProgress(definition.id);
                if (city == null || residents <= city.charterResidentMilestone) continue;
                city.charterResidentMilestone = residents; state.revision++;
            }
        }
        public Failure Accept(string id)
        {
            if (!Offered(id)) return Failure.NotReady;
            var definition = defs.cities.Single(c => c.id == id);
            if (!CanAccept(id)) return Failure.Locked;
            if (state.cards > int.MaxValue - definition.bridgeCards) return Failure.Invalid;
            RefreshMilestones();
            state.cities.Single(c => c.id == id).charterAccepted = true;
            state.money += definition.bridgeMoney; state.cards += definition.bridgeCards;
            state.City.stats.earned += definition.bridgeMoney;
            RewardHistory.Record(state, RewardSource.CityCharter, id, definition.invitationTitleKey,
                RewardHistory.Currency(definition.bridgeMoney, definition.bridgeCards));
            state.revision++; return Failure.None;
        }
        public Failure Travel(string id)
        {
            if (!state.introSeen || !Accessible(id)) return Failure.Locked;
            if (id == state.activeCity) return Failure.NotReady;
            state.activeCity = id; state.revision++; return Failure.None;
        }
    }
}
