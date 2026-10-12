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
            Refresh(); root.CameraRig.Inspect(root.Site(id));
        }
        public void CloseBuilding()
        { picker.SetActive(false); inspector.SetActive(false); inspectedId = null; if (root != null) root.CameraRig.EndInspection(); }
        public void HideForModal() { townMenu.SetActive(false); picker.SetActive(false); }
        public void ClosePicker() { picker.SetActive(false); }
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
            menuHint.text = root.Session.Progression.Ready ? "Eine Questbelohnung wartet auf dich." : "Ziehen oder wischen: die ganze Karte erkunden.\nAntippen: Gebäude ansehen. Zwei Finger: zoomen.";
            if (!BuildingOpen) return;
            var b = Building; var d = root.Definitions.Building(b.family); var manager = Manager;
            heading.text = BuildingName(b.id) + "\n<size=20>" + (b.level == 0 ? "Bauplatz" : "Level " + b.level) + "</size>";
            bool staffed = !string.IsNullOrEmpty(d.managerRole);
            staffPortrait.sprite = root.Presentation.art.Get(staffed ? "portrait_" + PresentationArt.CharacterForRole(d.managerRole) : "portrait_rocko");
            staffPortrait.color = manager == null && staffed ? new Color(.7f,.8f,.74f,.65f) : Color.white;
            staffName.text = manager == null ? staffed ? T(root.Definitions.Role(d.managerRole).nameKey) : "Burning Hills" : WorkerName(manager);
            staffBody.text = manager == null ? staffed ? b.level == 0 ? "Baue das Gebäude zuerst auf.\nDanach kannst du einen Worker einsetzen." : "Dieser Platz ist frei.\nMit + den passenden Worker auswählen." : d.kind == BuildingKind.Plot ? "Entscheide, wie dieser Platz\ndie Siedlung bereichern soll." : "Stadtverwaltung, Statistiken\nund neue Teammitglieder." : T("rarity." + manager.rarity) + " · Level " + manager.level + " · Rang " + manager.rank + "\n" + Traits(manager);
            staffAction.gameObject.SetActive(staffed); staffAction.interactable = b.level > 0 && !Busy(manager);
            staffMark.Mark = manager == null ? ActionMark.Shape.Plus : ActionMark.Shape.Cross;
            staffMark.color = manager == null ? new Color(.4f,1,.5f) : new Color(1,.35f,.3f);
            train.gameObject.SetActive(manager != null); rank.gameObject.SetActive(manager != null);
            if (manager != null)
            {
                Amount basis = new Amount(root.Definitions.workerLevelBaseCost) * manager.level * manager.rank;
                train.GetComponentInChildren<Text>().text = "Level erhöhen\n" + basis / root.Session.Workers.Bonus(b, WorkerAttribute.TrainingDiscount) + " Geld";
                rank.GetComponentInChildren<Text>().text = "Rang erhöhen\n" + basis / root.Session.Workers.Bonus(b, WorkerAttribute.RankDiscount) + " Geld · " + root.Definitions.workerRankCards + " Karten";
                train.interactable = manager.level < 100; rank.interactable = manager.rank < 10;
            }
            fleet.gameObject.SetActive(!string.IsNullOrEmpty(d.driverRole));
            fleetLabel.text = "Fuhrpark · " + root.Session.State.workers.Count(w => w.buildingId == b.id && w.slot > 0) + " / " + root.Session.Workers.Slots(b) + " Fahrer";
            status.text = b.level == 0 ? "Bereit für den Wiederaufbau" : b.buildingSeconds > 0 ? "Ausbau läuft · " + b.buildingSeconds + " s" : staffed && manager == null ? "Manueller Betrieb · Worker einsetzen" : T(b.bottleneck ?? "bottleneck.none");
            if(d.kind == BuildingKind.Bakery && b.level > 0 && b.buildingSeconds == 0)
            {
                var trip = manager == null ? null : root.Session.State.deliveries.FirstOrDefault(t => t.workerId == manager.id);
                if(manager == null) status.text = "Bäcker einsetzen · Butterabholung";
                else if(trip != null) status.text = trip.remainingSeconds == 0 ? "Lieferwagen wartet · Butterpuffer voll" :
                    (trip.fetching ? "Zur Butterfabrik · " : "Butter zur Bäckerei · ") + trip.remainingSeconds + " s";
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
            upgradeLabel.text = b.level >= 5000 ? "Maximales Level" : b.buildingSeconds > 0 ? "Ausbau beschleunigen" : (b.level == 0 ? "Aufbauen" : "Upgrade") + " · " + root.Session.Upgrades.Cost(b) + " Geld";
            work.GetComponentInChildren<Text>().text = d.kind == BuildingKind.TownHall ? "Rathaus öffnen" : d.kind == BuildingKind.Plot ? "Wohnhaus bauen" : d.kind == BuildingKind.Grow ? "Ernte einsammeln" : d.kind == BuildingKind.House || d.kind == BuildingKind.Park ? "Alle Tagesboni" : "Arbeit starten";
            work.interactable = d.kind == BuildingKind.TownHall || d.kind == BuildingKind.Plot || b.level > 0 && (d.kind != BuildingKind.Grow || b.raw > 0);
            work.gameObject.SetActive(d.kind != BuildingKind.Warehouse);
            if(d.kind == BuildingKind.Bakery && Busy(manager))work.interactable=false;
            talk.GetComponentInChildren<Text>().text = d.kind == BuildingKind.Plot ? "Park anlegen" : "Ansprechen";
            if (PickerOpen) RefreshPicker();
        }
        private InteractionRow Row(int index)
        { while (rowPool.Count <= index) rowPool.Add(Instantiate(rowPrefab, pickerContent)); rowPool[index].gameObject.SetActive(true); return rowPool[index]; }
        private void RefreshPicker()
        {
            var state = root.Session.State;
            // Include assignments and in-flight membership, so completing a trip unlocks controls immediately.
            string signature = fleetMode + ":" + selectedSlot + ":" + root.SelectedId + ":" + Building.level + ":" + string.Join(";", state.workers.Select(w => w.id + ":" + w.role + ":" + w.buildingId + ":" + w.slot + ":" + w.level + ":" + w.rank + ":" + Busy(w)));
            if (pickerSignature == signature) return; pickerSignature = signature;
            foreach (var row in rowPool) row.gameObject.SetActive(false);
            int count = 0;
            if (fleetMode)
            {
                pickerTitle.text = "Fuhrpark\n<size=20>" + BuildingName(root.SelectedId) + "</size>";
                int slots = root.Session.Workers.Slots(Building);
                pickerHint.text = slots == 0 ? "Ab Gebäudestufe 1 wird der erste Fahrerplatz frei. Weitere Plätze folgen mit den visuellen Ausbaustufen." : "Die Leitung organisiert den Versand. Fahrer beliefern automatisch die angeschlossenen Ziele. Unterwegs bleibt ihre Zuweisung gesperrt.";
                for (int i = 1; i <= slots; i++)
                {
                    int slot = i; var worker = state.workers.FirstOrDefault(w => w.buildingId == root.SelectedId && w.slot == slot);
                    var trip = worker == null ? null : state.deliveries.FirstOrDefault(t => t.workerId == worker.id);
                    string details = worker == null ? "Freier Platz für einen passenden Fahrer." : WorkerName(worker) + "\n" + (trip == null ? "Bereit für die nächste Lieferung" : "Unterwegs: " + BuildingName(root.Definitions.routes.Single(r => r.id == trip.route).to)) + "\n" + T("rarity." + worker.rarity) + " · Level " + worker.level + " · Rang " + worker.rank;
                    Row(count++).Bind(Portrait(root.Definitions.Building(Building.family).driverRole), "Fahrerplatz " + slot, details, trip != null ? "Unterwegs" : worker == null ? "Fahrer wählen" : "Wechseln", trip == null, () => OpenPicker(slot), worker != null && trip == null ? () => root.Command(() => root.Session.Workers.Remove(worker.id)) : (Action)null);
                }
            }
            else
            {
                string role = root.Session.Workers.SlotRole(Building, selectedSlot);
                pickerTitle.text = T(root.Definitions.Role(role).nameKey) + " wählen\n<size=20>" + (selectedSlot == 0 ? "Leitung" : "Fahrerplatz " + selectedSlot) + " · " + BuildingName(root.SelectedId) + "</size>";
                var occupant = state.workers.FirstOrDefault(w => w.buildingId == root.SelectedId && w.slot == selectedSlot);
                foreach (var worker in Candidates)
                {
                    bool same = worker == occupant; bool busy = Busy(worker) || Busy(occupant); string id = worker.id;
                    string assignment = string.IsNullOrEmpty(worker.buildingId) ? "Verfügbar" : "Einsatz: " + BuildingName(worker.buildingId);
                    Row(count++).Bind(Portrait(worker.role), WorkerName(worker), T("rarity." + worker.rarity) + " · Level " + worker.level + " · Rang " + worker.rank + "\n" + Traits(worker) + "\n" + assignment,
                        busy ? "Unterwegs" : same ? "Zugewiesen" : string.IsNullOrEmpty(worker.buildingId) ? "Zuweisen" : occupant == null ? "Hierher versetzen" : "Plätze tauschen", !busy && !same, () => Assign(id));
                }
            pickerHint.text = count == 0 ? "Noch kein passender Worker im Team. Aufgaben und der Spin unter Mein Team bringen neue Teammitglieder." : "Nur passende Worker für diesen Platz. Bereits eingesetzte Worker werden versetzt; besetzte kompatible Plätze werden getauscht.";
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
