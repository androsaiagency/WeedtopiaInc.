using System;
using System.Linq;

namespace Weedtopia.Domain
{
    [Serializable] public sealed class CityDefinition
    {
        public string id, nameKey, scenePath, sourceCity, invitationTitleKey, invitationBodyKey;
        public string invitationNoticeKey, invitationLockedKey, dialoguePrefix;
        // Combined mandates use a contemporaneous population total. sourceCity retains the original single-city charter contract.
        public string[] unlockSources = Array.Empty<string>();
        public int introducedVersion = 1, unlockResidents, bridgeMoney, bridgeCards;
    }

    /// <summary>City identity and access are domain data; Unity scenes are presentation destinations only.</summary>
    public static class CityCampaignRules
    {
        public static bool HasCharter(CityDefinition city) => !string.IsNullOrEmpty(city.sourceCity) || (city.unlockSources?.Length ?? 0) > 0;
        public static bool Combined(CityDefinition city) => (city.unlockSources?.Length ?? 0) > 0;
        public static string[] Sources(CityDefinition city) => Combined(city) ? city.unlockSources :
            string.IsNullOrEmpty(city.sourceCity) ? Array.Empty<string>() : new[] { city.sourceCity };
        public static string InvitationNoticeKey(CityDefinition city) => !string.IsNullOrEmpty(city.invitationNoticeKey) ? city.invitationNoticeKey :
            city.id == "seven_hills" ? "campaign.seven_hills_notice" : "campaign.notice";
        public static string InvitationLockedKey(CityDefinition city) => !string.IsNullOrEmpty(city.invitationLockedKey) ? city.invitationLockedKey :
            Combined(city) ? "campaign.combined_locked" : "campaign.locked";
        public static string DialoguePrefix(CityDefinition city) => !string.IsNullOrEmpty(city?.dialoguePrefix) ? city.dialoguePrefix :
            city?.id == "seven_hills" ? "sh.dialog." : city?.id == "big_rock_city" ? "br.dialog." : "dialog.";
        public static bool Accessible(GameDefinitions defs, GameState state, string id)
        {
            var city = state.cities.FirstOrDefault(c => c.id == id);
            if (city == null) return false;
            var definition = (defs.cities ?? Array.Empty<CityDefinition>()).FirstOrDefault(c => c.id == id);
            return definition == null || !HasCharter(definition) || city.charterAccepted;
        }
        public static void Validate(GameDefinitions defs)
        {
            var cities = defs.cities ?? Array.Empty<CityDefinition>();
            if (cities.Length == 0) return; // Historical content has only the original city.
            if (cities.Any(c => c == null || string.IsNullOrWhiteSpace(c.id)) || cities.Select(c => c.id).Distinct().Count() != cities.Length)
                throw new InvalidOperationException("Invalid city identities");
            if (!cities.Select(c => c.id).OrderBy(x => x).SequenceEqual(defs.instances.Select(i => i.cityId).Distinct().OrderBy(x => x)))
                throw new InvalidOperationException("City/instance catalog mismatch");
            foreach (var city in cities)
            {
                if (city.introducedVersion < 1 || city.introducedVersion > defs.version || string.IsNullOrWhiteSpace(city.scenePath) ||
                    !defs.texts.Any(t => t.key == city.nameKey) || city.bridgeMoney < 0 || city.bridgeCards < 0)
                    throw new InvalidOperationException("Invalid city destination " + city.id);
                if ((!string.IsNullOrEmpty(city.invitationNoticeKey) && !defs.texts.Any(t => t.key == city.invitationNoticeKey)) ||
                    (!string.IsNullOrEmpty(city.invitationLockedKey) && !defs.texts.Any(t => t.key == city.invitationLockedKey)))
                    throw new InvalidOperationException("Invalid city invitation text " + city.id);
                if (!HasCharter(city)) continue;
                var sources = Sources(city);
                if ((Combined(city) && !string.IsNullOrEmpty(city.sourceCity)) || sources.Distinct().Count() != sources.Length ||
                    sources.Any(id => string.IsNullOrWhiteSpace(id) || id == city.id ||
                        !cities.Any(c => c.id == id && c.introducedVersion < city.introducedVersion)) ||
                    city.unlockResidents < 1 || !defs.texts.Any(t => t.key == city.invitationTitleKey) || !defs.texts.Any(t => t.key == city.invitationBodyKey))
                    throw new InvalidOperationException("Invalid city charter " + city.id);
            }
        }
    }
}
