using System;
using System.Collections.Generic;
using Weedtopia.Domain;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>All city rows are authored and serialized; this adapter binds mandates and travel commands.</summary>
    public sealed class CityCampaignUi : MonoBehaviour
    {
        public Text title, invitationTitle, invitation, bridge, status, shared, burningProgress, rockProgress;
        public Image rockoPortrait;
        public Button accept, burningTravel, rockTravel, headerEntry, journalEntry;
        public GameObject offerBadge;
        public CityTravelRow[] travelRows = Array.Empty<CityTravelRow>();
        private GameRoot root;
        private readonly HashSet<string> notified = new HashSet<string>();
        private CityDefinition invitationCity;
        private string selectedInvitation;
        public string InvitationCityId => invitationCity?.id;
        public bool HasAuthoredTravelRows => travelRows != null && travelRows.Length > 0;
        public void ShowInvitation(string cityId)
        {
            selectedInvitation = cityId;
            root.Ui.ShowCities();
        }
        public void Initialize(GameRoot game)
        {
            root = game;
            rockoPortrait.sprite = root.Presentation.art.Get("portrait_rocko");
            headerEntry.onClick.AddListener(root.Ui.ShowCities); journalEntry.onClick.AddListener(root.Ui.ShowCities);
            accept.onClick.AddListener(() =>
            {
                if (invitationCity == null) return;
                string target = invitationCity.id;
                root.CampaignCommand(() => root.Session.Campaign.Accept(target)); Refresh();
            });
            if (HasAuthoredTravelRows)
            {
                foreach (var row in travelRows) if (row != null) row.Initialize(root);
            }
            else
            {
                // Existing scenes remain operable until their explicit authoring upgrade installs all city rows.
                burningTravel.onClick.AddListener(() => root.TravelToCity("burning_hills"));
                rockTravel.onClick.AddListener(() => root.TravelToCity("big_rock_city"));
            }
            RefreshLanguage();
        }
        public void RefreshLanguage()
        {
            title.text=T("campaign.title");
            shared.text=T(root.Definitions.texts.Any(t=>t.key=="campaign.shared_cities")?"campaign.shared_cities":"campaign.shared");
            accept.GetComponentInChildren<Text>().text=T("campaign.accept");Refresh();
        }
        private string T(string key) => root.Definitions.Text(key);
        public void Refresh()
        {
            if (root?.Session == null) return;
            var session = root.Session;
            var mandates = (root.Definitions.cities ?? Array.Empty<CityDefinition>()).Where(CityCampaignRules.HasCharter).ToArray();
            invitationCity = mandates.FirstOrDefault(c => c.id == selectedInvitation && !session.Campaign.Accessible(c.id)) ??
                mandates.FirstOrDefault(c => session.Campaign.Offered(c.id)) ??
                mandates.FirstOrDefault(c => !session.Campaign.Accessible(c.id)) ??
                mandates.LastOrDefault();
            offerBadge.SetActive(session.Campaign.AnyOffered); headerEntry.interactable = !root.TravelLoading;
            if (invitationCity != null)
            {
                bool offered = session.Campaign.Offered(invitationCity.id), owned = session.Campaign.Accessible(invitationCity.id);
                invitationTitle.text = T(invitationCity.invitationTitleKey); invitation.text = T(invitationCity.invitationBodyKey);
                accept.gameObject.SetActive(!owned); accept.interactable = session.Campaign.CanAccept(invitationCity.id) && !root.TravelLoading;
                bridge.text = string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture, T("campaign.bridge"), invitationCity.bridgeMoney, invitationCity.bridgeCards);
                string noticeKey = CityCampaignRules.InvitationNoticeKey(invitationCity);
                status.text = owned ? T("campaign.accepted") : offered ? T(noticeKey) :
                    string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,
                        T(CityCampaignRules.InvitationLockedKey(invitationCity)),
                        invitationCity.unlockResidents, session.Campaign.UnlockProgress(invitationCity.id));
                if (offered && notified.Add(invitationCity.id)) root.Ui.Notify(T(noticeKey));
            }
            else accept.gameObject.SetActive(false);
            if (HasAuthoredTravelRows)
                foreach (var row in travelRows) if (row != null) row.Refresh();
            else
            {
                CityTravelRow.Refresh(root, "burning_hills", burningProgress, burningTravel);
                CityTravelRow.Refresh(root, "big_rock_city", rockProgress, rockTravel);
            }
        }
    }
}
