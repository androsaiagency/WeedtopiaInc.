using System.Linq;
using Weedtopia.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>A serialized travel destination. Binding and refresh never create presentation objects.</summary>
    public sealed class CityTravelRow : MonoBehaviour
    {
        public string cityId;
        public Text progress;
        public Button travel;
        private GameRoot root;
        public void Initialize(GameRoot game)
        {
            root = game;
            travel.onClick.AddListener(() =>
            {
                if (root.Session.Campaign.Accessible(cityId)) root.TravelToCity(cityId);
                else if (root.Session.Campaign.Offered(cityId)) root.Ui.Campaign.ShowInvitation(cityId);
            });
        }
        public void Refresh() => Refresh(root, cityId, progress, travel);
        public static void Refresh(GameRoot root, string cityId, Text progress, Button travel)
        {
            if (root?.Session == null || progress == null || travel == null) return;
            var session = root.Session; var city = session.State.cities.FirstOrDefault(c => c.id == cityId);
            var definition = root.Definitions.cities.FirstOrDefault(c => c.id == cityId);
            if (city == null || definition == null) { travel.interactable = false; return; }
            string name = root.Definitions.Text(definition.nameKey);
            var culture = GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture;
            progress.text = name + "\n" + string.Format(culture, root.Definitions.Text("campaign.progress"),
                session.Simulation.Residents(cityId), city.questIndex, root.Definitions.QuestsFor(cityId).Length);
            bool accessible = session.Campaign.Accessible(cityId), offered = session.Campaign.Offered(cityId);
            travel.interactable = !root.TravelLoading && (accessible || offered) && cityId != session.State.activeCity;
            string travelKey = root.Definitions.texts.Any(text => text.key == "campaign.travel_action") ? "campaign.travel_action" : "campaign.travel";
            travel.GetComponentInChildren<Text>().text = cityId == session.State.activeCity ? root.Definitions.Text("campaign.current") :
                !accessible && offered ? root.Definitions.Text("campaign.view_offer") :
                string.Format(culture, root.Definitions.Text(travelKey), name);
        }
    }
}
