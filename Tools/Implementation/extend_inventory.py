"""One-off follow-up: lossless denomination exchange for long construction speedups."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
def edit(name, old, new):
    path = root / name
    text = path.read_text(encoding='utf-8-sig')
    assert old in text, (name, old[:80])
    path.write_text(text.replace(old,new),encoding='utf-8')
base = 'Assets/Weedtopia/Scripts/'
p = base+'Runtime/Application/InventoryService.cs'
edit(p, 'public int UnusedSeconds { get; internal set; }', 'public int UnusedSeconds { get; internal set; }\n        public string SplitItemId { get; internal set; }\n        public int SplitCount { get; internal set; }')
edit(p, '        public void Cancel() { pending = null; }', '''        public ItemDefinition SplitTarget(string id)
        {
            var item = defs.items?.FirstOrDefault(i => i.id == id);
            return item?.effect == ItemEffect.ConstructionTime ? defs.items.Where(i => i.effect == ItemEffect.ConstructionTime &&
                i.value < item.value && item.value % i.value == 0).OrderByDescending(i => i.value).FirstOrDefault() : null;
        }
        public Failure OfferSplit(string id, out ItemUseOffer offer)
        {
            pending = null; offer = null;
            var smaller = SplitTarget(id);
            if (smaller == null || Count(id) < 1) return Failure.NotReady;
            int count = defs.items.Single(i => i.id == id).value / smaller.value;
            if (Count(smaller.id) > int.MaxValue - count) return Failure.Invalid;
            pending = new ItemUseOffer { Token = Guid.NewGuid().ToString("N"), ItemId = id, city = state.activeCity,
                SplitItemId = smaller.id, SplitCount = count };
            offer = pending; return Failure.None;
        }
        public void Cancel() { pending = null; }''')
edit(p, '            BuildingState building = null;', '''            if (offer.SplitItemId != null)
            {
                var smaller = SplitTarget(item.id);
                if (smaller == null || smaller.id != offer.SplitItemId || Count(smaller.id) > int.MaxValue - offer.SplitCount) return Failure.StaleOffer;
                var exchanged = new List<RewardItem>();
                InventoryItems.Grant(state, new[] { new ItemStack { id = smaller.id, count = offer.SplitCount } }, exchanged);
                Consume(item, exchanged); return Failure.None;
            }
            BuildingState building = null;''')
edit(p, '            var stack = state.inventory.Single(i => i.id == offer.ItemId);', '''            Consume(item, rewards); return Failure.None;
        }
        private void Consume(ItemDefinition item, List<RewardItem> rewards)
        {
            var stack = state.inventory.Single(i => i.id == item.id);''')
edit(p, '            state.revision++; return Failure.None;\n        }\n    }', '            state.revision++;\n        }\n    }')
p = base+'Runtime/Unity/InventoryDetailView.cs'
edit(p, 'public Button action, cancel, previousChoice, nextChoice;', 'public Button action, cancel, previousChoice, nextChoice, split;\n        public Text splitLabel;')
edit(p, 'action.onClick.AddListener(Activate);', 'action.onClick.AddListener(Activate);\n            split.onClick.AddListener(PrepareSplit);')
edit(p, '            action.gameObject.SetActive(selected != null);', '            split.gameObject.SetActive(false);\n            action.gameObject.SetActive(selected != null);')
edit(p, '            if (confirming) { choiceHeading.text = T("inventory.confirm_hint"); }', '''            var smaller = session.Inventory.SplitTarget(item.id);
            split.gameObject.SetActive(smaller != null && !confirming);
            if (smaller != null)
            {
                split.interactable = session.Inventory.Count(item.id) > 0;
                splitLabel.text = string.Format(T("inventory.split"), item.value / smaller.value, InventoryText.Duration(smaller.value));
            }
            if (confirming)
            {
                choiceHeading.text = T("inventory.confirm_hint");
                if (offer.SplitItemId != null)
                {
                    action.interactable = true; choiceLabel.text = "";
                    effect.text = string.Format(T("inventory.split_preview"), InventoryText.Duration(item.value), offer.SplitCount,
                        InventoryText.Duration(root.Definitions.items.First(i => i.id == offer.SplitItemId).value));
                }
            }''')
edit(p, 'bool speedup = Definition.effect == ItemEffect.ConstructionTime;', 'bool speedup = Definition.effect == ItemEffect.ConstructionTime && offer.SplitItemId == null;')
edit(p, '        private void ReleasePause()', '''        private void PrepareSplit()
        {
            if (selected?.kind != RewardKind.Item || confirming) return;
            var result = root.Session.Inventory.OfferSplit(selected.key, out offer);
            if (result != Failure.None) { root.Ui.Notify(T("failure." + result)); return; }
            pausedSession = root.Session; priorPaused = pausedSession.Paused; pausedSession.Paused = true;
            confirming = true; Refresh();
        }
        private void ReleasePause()''')
p = base+'Editor/GameSetup/InventoryEvidence.cs'
edit(p, 'b.family=="farm_grow"', 'b.family=="cultivation_plot"')
edit(p, '                ()=>{ui.SelectCategory(1);Inspect("specialist_contract");', '                ()=>{Inspect("build_24h");ui.details.split.onClick.Invoke();root.Refresh();capture("04b_split_confirmation_1920");},\n                ()=>{ui.details.cancel.onClick.Invoke();ui.SelectCategory(1);Inspect("specialist_contract");')
p = base+'Tests/EditMode/InventoryTests.cs'
edit(p, 'Grant("build_5m",3);Grant("cash_crate");Use("cash_crate");var b=Construction(700);Use("build_5m",b.id);', 'Grant("build_1m",3);Grant("cash_crate");Use("cash_crate");var b=Construction(100);Use("build_1m",b.id);')
edit(p, 'InventoryItems.Count(restored,"build_5m")', 'InventoryItems.Count(restored,"build_1m")')
edit(p, 'buildingSeconds,Is.EqualTo(400)', 'buildingSeconds,Is.EqualTo(40)')
edit(p, 'InventoryItems.Count(store.Load().State,"build_5m")', 'InventoryItems.Count(store.Load().State,"build_1m")')
edit(p, '        [Test] public void NumericOverflowDoesNotConsumeThePack()', '''        [TestCase("build_5m")][TestCase("build_15m")][TestCase("build_30m")][TestCase("build_1h")]
        [TestCase("build_5h")][TestCase("build_10h")][TestCase("build_24h")]
        public void SplittingPreservesEverySecondWithoutAConstructionAndIsSingleUse(string id)
        {
            Grant(id,2);var item=defs.items.Single(i=>i.id==id);var before=state.money;
            Assert.That(session.Inventory.OfferSplit(id,out var offer),Is.EqualTo(Failure.None));
            Assert.That(session.Inventory.Count(id),Is.EqualTo(2));
            Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.None));
            Assert.That(session.Inventory.Count(id),Is.EqualTo(1));
            var smaller=defs.items.Single(i=>i.id==offer.SplitItemId);
            Assert.That(session.Inventory.Count(smaller.id)*smaller.value,Is.EqualTo(item.value));
            Assert.That(session.Inventory.Confirm(offer.Token),Is.EqualTo(Failure.StaleOffer));
            Assert.That(state.money,Is.EqualTo(before));Assert.That(state.simulationSeconds,Is.Zero);
            SaveValidator.Validate(state,defs);
        }
        [Test] public void SmallestBoosterCannotBeSplitAndSplitOverflowCannotSpend()
        {
            Grant("build_1m");Assert.That(session.Inventory.OfferSplit("build_1m",out _),Is.EqualTo(Failure.NotReady));
            Grant("build_5m");state.inventory.Single(i=>i.id=="build_1m").count=int.MaxValue;
            Assert.That(session.Inventory.OfferSplit("build_5m",out _),Is.EqualTo(Failure.Invalid));Assert.That(session.Inventory.Count("build_5m"),Is.EqualTo(1));
        }
        [Test] public void NumericOverflowDoesNotConsumeThePack()''')
# Add the split button to the already authored detail prefab, preserving every existing transform.
p = base+'Editor/GameSetup/InventoryAuthoring.cs'
edit(p, '                var serialized=new SerializedObject(root.Ui);', '''                var detailRoot=PrefabUtility.LoadPrefabContents(DetailPath);
                try
                {
                    var detail=detailRoot.GetComponent<InventoryDetailView>();
                    if(detail.split==null)
                    {
                        detail.split=Button("Split",detailRoot.transform,"",new Vector2(0,-331),new Vector2(366,30));
                        detail.splitLabel=detail.split.GetComponentInChildren<Text>();detail.splitLabel.fontSize=16;
                        PrefabUtility.SaveAsPrefabAsset(detailRoot,DetailPath);
                    }
                }
                finally{PrefabUtility.UnloadPrefabContents(detailRoot);}
                var serialized=new SerializedObject(root.Ui);''')
p = base+'Tests/PlayMode/RewardPresentationTests.cs'
edit(p, '        [UnityTest] public IEnumerator NewGameClearsPendingCelebrationAndHistory()', '''        [UnityTest] public IEnumerator SplitIsClickableWithoutConstructionAndKeepsAllTimeInSavedInventory()
        {
            GrantItem("build_24h");root.Ui.ShowCollection();var ui=root.Ui.Collection;
            ui.tiles[0].button.onClick.Invoke();yield return null;
            Assert.That(ui.details.action.interactable,Is.False);Assert.That(ui.details.split.interactable,Is.True);
            foreach(var button in new[]{ui.tiles[0].button,ui.categoryButtons[1],ui.categoryButtons[2],ui.categoryButtons[3],ui.details.split})
            {
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
                var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                Assert.That(hits.First().gameObject.GetComponentInParent<Button>(),Is.EqualTo(button),button.name+" is covered");
            }
            ui.details.split.onClick.Invoke();yield return null;
            Assert.That(ui.details.Confirming,Is.True);Assert.That(ui.details.effect.text,Does.Contain("24"));
            ui.details.action.onClick.Invoke();yield return null;
            Assert.That(root.Session.Inventory.Count("build_24h"),Is.Zero);Assert.That(root.Session.Inventory.Count("build_1h"),Is.EqualTo(24));
            Assert.That(root.Ui.Rewards.IsOpen,Is.True);root.Ui.Rewards.DismissAll();Assert.That(root.Session.Paused,Is.False);
            root.Continue();yield return null;Assert.That(root.Session.Inventory.Count("build_1h"),Is.EqualTo(24));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NewGameClearsPendingCelebrationAndHistory()''')
print('Lossless speedup splitting integrated.')
