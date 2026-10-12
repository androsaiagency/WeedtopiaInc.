using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Contextual building inspection; all fixed UI is authored in the scene, repeated data uses a prefab pool.</summary>
    public sealed class BuildingInteractionUi : MonoBehaviour
    {
        public GameObject inspector, picker, townMenu;
        public Button launcher, closeBuilding, closePicker, closeTown, staffAction, fleet, upgrade, work, talk, train, rank, overview, inventory;
        public Image staffPortrait;
        public ActionMark staffMark;
        public Text heading, staffName, staffBody, status, upgradeLabel, fleetLabel, pickerTitle, pickerHint, menuHint;
        public Text[] metricLabels, metricCurrent, metricNext;
        public GameObject[] metricArrows;
        public RectTransform pickerContent;
        public ScrollRect pickerScroll;
        public InteractionRow rowPrefab;
        private readonly List<InteractionRow> rowPool = new List<InteractionRow>();
        private GameRoot root;
        private int selectedSlot;
        private bool fleetMode;
        private string pickerSignature;
        private string inspectedId;
        public bool BuildingOpen => inspector.activeSelf;
        public bool PickerOpen => picker.activeSelf;
        public bool BlocksWorld => picker.activeSelf || townMenu.activeSelf;
        public int SelectedSlot => selectedSlot;
        public IEnumerable<WorkerState> Candidates => root.Session.State.workers.Where(w => w.role == root.Session.Workers.SlotRole(Building, selectedSlot))
            .OrderBy(w => !string.IsNullOrEmpty(w.buildingId)).ThenByDescending(w => w.rarity).ThenBy(w => w.id);
        private BuildingState Building => root.Session.State.Find(root.SelectedId);
        private WorkerState Manager => root.Session.State.workers.FirstOrDefault(w => w.buildingId == root.SelectedId && w.slot == 0);
        private string T(string key) => root.Definitions.Text(key);
        private Sprite Portrait(string role) => root.Presentation.art.Get("portrait_" + PresentationArt.CharacterForRole(role));
        private bool Busy(WorkerState worker) => worker != null && root.Session.State.deliveries.Any(d => d.workerId == worker.id);
        private string BuildingName(string id)
        {
            var b = root.Session.State.Find(id); string name = T(root.Definitions.Building(b.family).nameKey);
            string suffix = id.Substring(id.LastIndexOf('_') + 1);
            return int.TryParse(suffix, out int number) ? name + " " + number : name;
        }
        private string WorkerName(WorkerState w) => T("speaker." + PresentationArt.CharacterForRole(w.role)).Split('·')[0].Trim() + " · " + T(root.Definitions.Role(w.role).nameKey);
        private string Traits(WorkerState w) => string.Join("\n", w.attributes.Select(a => T("attribute." + (int)a) + "  +" + (root.Session.Workers.Effect(w,a)*100).ToString("0.#") + "%"));
        private void Forward(string id) => root.Ui.Element<Button>(id).onClick.Invoke();
        public void Initialize(GameRoot game)
        {
            root = game;
            // Serialized legacy objects stay in the inactive archive for reference-safe migration.
            launcher.gameObject.SetActive(false);
            closeBuilding.onClick.AddListener(CloseBuilding); closePicker.onClick.AddListener(ClosePicker);
            staffAction.onClick.AddListener(() => { var w = Manager; if (w == null) OpenPicker(0); else root.Command(() => root.Session.Workers.Remove(w.id)); });
            fleet.onClick.AddListener(OpenFleet);
            upgrade.onClick.AddListener(() => Forward(Building.buildingSeconds > 0 ? "ui.finish" : "ui.upgrade"));
            train.onClick.AddListener(() => Improve(false)); rank.onClick.AddListener(() => Improve(true));
            work.onClick.AddListener(() =>
            {
                var kind = root.Definitions.Building(Building.family).kind;
                if (kind == BuildingKind.TownHall) root.Ui.ShowTownHall();
                else Forward(kind == BuildingKind.Plot ? "ui.house" : kind == BuildingKind.Grow ? "ui.harvest" : kind == BuildingKind.House || kind == BuildingKind.Park ? "ui.daily" : "ui.work");
            });
            talk.onClick.AddListener(() => Forward(root.Definitions.Building(Building.family).kind == BuildingKind.Plot ? "ui.park" : "ui.talk"));
            overview.onClick.AddListener(() => { townMenu.SetActive(false); CloseBuilding(); root.CameraRig.ShowOverview(); });
            inventory.onClick.AddListener(() => { townMenu.SetActive(false); root.Ui.ShowWorkers(); });
            inspector.SetActive(false); picker.SetActive(false); townMenu.SetActive(false);
        }
        private void Improve(bool promote)
        { var w = Manager; if (w != null) root.Command(() => root.Session.Workers.Improve(w.id, promote)); }
        public void OpenBuilding(string id)
        {
            if (root.Session == null) return;
            var state = root.Session.State.Find(id);
            if (root.Definitions.Building(state.family).zone > root.Session.State.City.unlocked) { root.Select(id); return; }
            root.Select(id); inspectedId = id; townMenu.SetActive(false); picker.SetActive(false); inspector.SetActive(true);
            var layout = inspector.GetComponent<BuildingInspectorLayout>();
            if (layout != null) layout.scroll.verticalNormalizedPosition = 1;
            Refresh(); root.CameraRig.Inspect(root.Site(id));
        }
        public void CloseBuilding()
        { picker.SetActive(false); inspector.SetActive(false); inspectedId = null; if (root != null) root.CameraRig.EndInspection(); }
        public void HideForModal() { townMenu.SetActive(false); picker.SetActive(false); }
        public void ClosePicker() { picker.SetActive(false); root.Refresh(); }
        public void OpenPicker(int slot)
        {
            if (root.Session == null || Building.level < 1 || slot < 0 || slot > root.Session.Workers.Slots(Building)) return;
            if (string.IsNullOrEmpty(root.Session.Workers.SlotRole(Building, slot))) return;
            selectedSlot = slot; fleetMode = false; picker.SetActive(true); pickerSignature = null; RefreshPicker(); pickerScroll.verticalNormalizedPosition = 1;
        }
        public void OpenFleet()
        { fleetMode = true; picker.SetActive(true); pickerSignature = null; RefreshPicker(); pickerScroll.verticalNormalizedPosition = 1; }
        public void Assign(string workerId)
        {
            var worker = Candidates.FirstOrDefault(w => w.id == workerId);
            if (worker == null || Busy(worker)) return;
            var occupant = root.Session.State.workers.FirstOrDefault(w => w.buildingId == root.SelectedId && w.slot == selectedSlot);
            if (Busy(occupant)) return;
            if (root.Command(() => root.Session.Workers.Assign(workerId, root.SelectedId, selectedSlot)) == Failure.None)
            { if (selectedSlot > 0) OpenFleet(); else ClosePicker(); }
        }
        public void Refresh()
        {
            if (root.Session == null) return;
            if (BuildingOpen && inspectedId != root.SelectedId) { CloseBuilding(); return; }
            menuHint.text = root.Session.Progression.Ready ? GameText.Get("presentation.buildinginteractionui.eine_questbelohnung_wartet_auf_dich.270379fe") : GameText.Get("presentation.buildinginteractionui.ziehen_oder_wischen_die_ganze_karte_erku.39d97baf");
            if (!BuildingOpen) return;
            var b = Building; var d = root.Definitions.Building(b.family); var manager = Manager;
            heading.text = BuildingName(b.id) + "\n" + (b.level == 0 ? T("inspection.build_site") : GameText.Format("format.level",b.level));
            bool staffed = !string.IsNullOrEmpty(d.managerRole);
            staffPortrait.sprite = root.Presentation.art.Get(staffed ? "portrait_" + PresentationArt.CharacterForRole(d.managerRole) : "portrait_rocko");
            staffPortrait.color = manager == null && staffed ? new Color(.7f,.8f,.74f,.65f) : Color.white;
            staffName.text = manager == null ? staffed ? T(root.Definitions.Role(d.managerRole).nameKey) : T("city." + root.Session.State.activeCity) : WorkerName(manager);
            staffBody.text = manager == null ? staffed ? b.level == 0 ? GameText.Get("presentation.buildinginteractionui.baue_das_geb_ude_zuerst_auf_danach_kanns.380ddf1c") : GameText.Get("presentation.buildinginteractionui.dieser_platz_ist_frei_mit_den_passenden_.375616ad") : d.kind == BuildingKind.Plot ? GameText.Get("presentation.buildinginteractionui.entscheide_wie_dieser_platz_die_siedlung.16d76ffa") : GameText.Get("presentation.buildinginteractionui.stadtverwaltung_statistiken_und_neue_tea.c324faf6") : GameText.Format("format.worker",T("rarity."+manager.rarity),manager.level,manager.rank,Traits(manager));
            staffAction.gameObject.SetActive(staffed); staffAction.interactable = b.level > 0 && !Busy(manager);
            staffMark.Mark = manager == null ? ActionMark.Shape.Plus : ActionMark.Shape.Cross;
            staffMark.color = manager == null ? new Color(.4f,1,.5f) : new Color(1,.35f,.3f);
            train.gameObject.SetActive(manager != null); rank.gameObject.SetActive(manager != null);
            if (manager != null)
            {
                Amount basis = new Amount(root.Definitions.workerLevelBaseCost) * manager.level * manager.rank;
                train.GetComponentInChildren<Text>().text = GameText.Get("presentation.buildinginteractionui.level_erh_hen.d3002787") + basis / root.Session.Workers.Bonus(b, WorkerAttribute.TrainingDiscount) + GameText.Get("presentation.buildinginteractionui.geld.c29cffbd");
                rank.GetComponentInChildren<Text>().text = GameText.Get("presentation.buildinginteractionui.rang_erh_hen.8d0e1dce") + basis / root.Session.Workers.Bonus(b, WorkerAttribute.RankDiscount) + GameText.Get("format.money_separator") + root.Definitions.workerRankCards + GameText.Get("presentation.buildinginteractionui.karten.dcd270e7");
                train.interactable = manager.level < 100; rank.interactable = manager.rank < 10;
            }
            fleet.gameObject.SetActive(!string.IsNullOrEmpty(d.driverRole));
            fleetLabel.text = GameText.Format("inspection.fleet",root.Session.State.workers.Count(w=>w.buildingId==b.id&&w.slot>0),root.Session.Workers.Slots(b));
            status.text = b.buildingSeconds > 0 ? (b.level == 0 ? GameText.Get("presentation.buildinginteractionui.aufbau_l_uft.7860d14e") : GameText.Get("presentation.buildinginteractionui.ausbau_l_uft.4e7d77f7")) + BuildingInspectionMetrics.FormatDuration(b.buildingSeconds) : b.level == 0 ? GameText.Get("presentation.buildinginteractionui.bereit_f_r_den_wiederaufbau.b3324b47") : staffed && manager == null ? GameText.Get("presentation.buildinginteractionui.manueller_betrieb_worker_einsetzen.5c487d10") : T(b.bottleneck ?? "bottleneck.none");
            if(d.kind == BuildingKind.Bakery && b.level > 0 && b.buildingSeconds == 0)
            {
                var trip = manager == null ? null : root.Session.State.deliveries.FirstOrDefault(t => t.workerId == manager.id);
                if(manager == null) status.text = GameText.Get("presentation.buildinginteractionui.b_cker_einsetzen_butterabholung.2f653004");
                else if(trip != null) status.text = trip.remainingSeconds == 0 ? GameText.Get("presentation.buildinginteractionui.lieferwagen_wartet_butterpuffer_voll.7e890528") :
                    (trip.fetching ? GameText.Get("presentation.buildinginteractionui.zur_butterfabrik.2bb2688a") : GameText.Get("presentation.buildinginteractionui.butter_zur_b_ckerei.fd339713")) + trip.remainingSeconds + " s";
            }
            var metrics = BuildingInspectionMetrics.Read(root, b);
            for (int i = 0; i < metricLabels.Length; i++)
            {
                bool active = i < metrics.Count; metricLabels[i].transform.parent.gameObject.SetActive(active);
                if (!active) continue; var m = metrics[i]; metricLabels[i].text = m.Label; metricCurrent[i].text = m.Current;
                metricNext[i].text = m.Next ?? ""; metricArrows[i].SetActive(m.Next != null);
            }
            upgrade.gameObject.SetActive(d.kind != BuildingKind.TownHall && d.kind != BuildingKind.Plot);
            upgrade.interactable = b.level < 5000;
            upgradeLabel.text = b.level >= 5000 ? GameText.Get("presentation.buildinginteractionui.maximales_level.93b772e4") : b.buildingSeconds > 0 ? GameText.Get("presentation.buildinginteractionui.ausbau_beschleunigen.c6a1191e") : GameText.Format(b.level==0?"inspection.build":"inspection.upgrade",root.Session.Upgrades.Cost(b));
            work.GetComponentInChildren<Text>().text = d.kind == BuildingKind.TownHall ? GameText.Get("presentation.buildinginteractionui.rathaus_ffnen.c9232839") : d.kind == BuildingKind.Plot ? GameText.Get("presentation.buildinginteractionui.wohnhaus_bauen.e69be2ba") : d.kind == BuildingKind.Grow ? GameText.Get("ui.harvest") : d.kind == BuildingKind.House || d.kind == BuildingKind.Park ? GameText.Get("presentation.buildinginteractionui.alle_tagesboni.d96b7ba0") : GameText.Get("presentation.buildinginteractionui.arbeit_starten.e6a1e5fe");
            work.interactable = d.kind == BuildingKind.TownHall || d.kind == BuildingKind.Plot || b.level > 0 && (d.kind != BuildingKind.Grow || b.raw > 0);
            work.gameObject.SetActive(d.kind != BuildingKind.Warehouse);
            if(d.kind == BuildingKind.Bakery && Busy(manager))work.interactable=false;
            talk.GetComponentInChildren<Text>().text = d.kind == BuildingKind.Plot ? GameText.Get("presentation.buildinginteractionui.park_anlegen.e82b9998") : GameText.Get("inspection.talk");
            if (PickerOpen) RefreshPicker();
        }
        private InteractionRow Row(int index)
        { while (rowPool.Count <= index) rowPool.Add(Instantiate(rowPrefab, pickerContent)); rowPool[index].gameObject.SetActive(true); return rowPool[index]; }
        private void RefreshPicker()
        {
            var state = root.Session.State;
            // Include assignments and in-flight membership, so completing a trip unlocks controls immediately.
            string signature = root.Localization.Language + ":" + fleetMode + ":" + selectedSlot + ":" + root.SelectedId + ":" + Building.level + ":" + string.Join(";", state.workers.Select(w => w.id + ":" + w.role + ":" + w.buildingId + ":" + w.slot + ":" + w.level + ":" + w.rank + ":" + Busy(w)));
            if (pickerSignature == signature) return; pickerSignature = signature;
            foreach (var row in rowPool) row.gameObject.SetActive(false);
            int count = 0;
            if (fleetMode)
            {
                pickerTitle.text = GameText.Get("inspection.fleet_title") + BuildingName(root.SelectedId);
                int slots = root.Session.Workers.Slots(Building);
                pickerHint.text = slots == 0 ? GameText.Get("presentation.buildinginteractionui.baue_das_geb_ude_auf_stufe_1_aus_um_eine.a0ade76d") : GameText.Get("presentation.buildinginteractionui.fahrer_liefern_automatisch_unterwegs_ist.7784ecb7");
                for (int i = 1; i <= slots; i++)
                {
                    int slot = i; var worker = state.workers.FirstOrDefault(w => w.buildingId == root.SelectedId && w.slot == slot);
                    var trip = worker == null ? null : state.deliveries.FirstOrDefault(t => t.workerId == worker.id);
                    string details = worker == null ? GameText.Get("presentation.buildinginteractionui.freier_platz_f_r_einen_passenden_fahrer.eab71a4b") : WorkerName(worker) + "\n" + (trip == null ? GameText.Get("presentation.buildinginteractionui.bereit_f_r_die_n_chste_lieferung.f368d705") : GameText.Get("presentation.buildinginteractionui.0daa7e801161") + BuildingName(root.Definitions.routes.Single(r => r.id == trip.route).to)) + "\n" + T("rarity." + worker.rarity) + GameText.Get("presentation.buildinginteractionui.level.645d01b9") + worker.level + GameText.Get("presentation.buildinginteractionui.rang.dcd500e0") + worker.rank;
                    Row(count++).Bind(Portrait(root.Definitions.Building(Building.family).driverRole), GameText.Get("presentation.buildinginteractionui.18be3e7aa4fa") + slot, details, trip != null ? "Unterwegs" : worker == null ? GameText.Get("presentation.buildinginteractionui.fahrer_w_hlen.772f0762") : "Wechseln", trip == null, () => OpenPicker(slot), worker != null && trip == null ? () => root.Command(() => root.Session.Workers.Remove(worker.id)) : (Action)null);
                }
            }
            else
            {
                string role = root.Session.Workers.SlotRole(Building, selectedSlot);
                pickerTitle.text = T(root.Definitions.Role(role).nameKey) + GameText.Get("presentation.buildinginteractionui.w_hlen.1b6b58a2") + (selectedSlot == 0 ? GameText.Get("presentation.buildinginteractionui.leitung.88bcd24d") : GameText.Get("presentation.buildinginteractionui.18be3e7aa4fa") + selectedSlot) + " · " + BuildingName(root.SelectedId);
                var occupant = state.workers.FirstOrDefault(w => w.buildingId == root.SelectedId && w.slot == selectedSlot);
                foreach (var worker in Candidates)
                {
                    bool same = worker == occupant; bool busy = Busy(worker) || Busy(occupant); string id = worker.id;
                    string assignment = string.IsNullOrEmpty(worker.buildingId) ? GameText.Get("presentation.buildinginteractionui.verf_gbar.3aa55273") : GameText.Get("presentation.buildinginteractionui.9e2d37bfb3f8") + BuildingName(worker.buildingId);
                    Row(count++).Bind(Portrait(worker.role), WorkerName(worker), T("rarity." + worker.rarity) + GameText.Get("presentation.buildinginteractionui.level.645d01b9") + worker.level + GameText.Get("presentation.buildinginteractionui.rang.dcd500e0") + worker.rank + "\n" + Traits(worker) + "\n" + assignment,
                        busy ? "Unterwegs" : same ? "Zugewiesen" : string.IsNullOrEmpty(worker.buildingId) ? "Zuweisen" : occupant == null ? GameText.Get("presentation.buildinginteractionui.hierher_versetzen.ca38245a") : GameText.Get("presentation.buildinginteractionui.pl_tze_tauschen.12731a36"), !busy && !same, () => Assign(id));
                }
            pickerHint.text = count == 0 ? GameText.Get("presentation.buildinginteractionui.noch_kein_passender_worker_aufgaben_und_.e722ddff") : GameText.Get("presentation.buildinginteractionui.passende_worker_w_hlen_belegte_pl_tze_we.27165b5a");
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(pickerContent);
        }
        private void Update()
        {
            if (root == null || root.Session == null || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (root.Ui.LegacyModalOpen) root.Ui.Close(); else if (PickerOpen) ClosePicker(); else if (BuildingOpen) CloseBuilding(); else townMenu.SetActive(!townMenu.activeSelf);
        }
    }
}
