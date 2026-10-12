"""Explicit, one-off source migration. Baseline is in Docs/InventoryRevision/Originals."""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
scripts = root / 'Assets/Weedtopia/Scripts'
def edit(path, old, new):
    p = scripts / path
    text = p.read_text(encoding='utf-8-sig')
    assert old in text, (path, old[:80])
    p.write_text(text.replace(old, new), encoding='utf-8')

edit('Runtime/Domain/GameState.cs', 'public long rewardSequence;', 'public List<ItemStack> inventory = new List<ItemStack>();\n        public long rewardSequence;')
edit('Runtime/Domain/RewardHistory.cs', 'District, VipOrders }', 'District, VipOrders, Item }')
edit('Runtime/Domain/RewardHistory.cs', 'Daily, Recruitment }', 'Daily, Recruitment, Inventory }')
for p in ['Runtime/Domain/Definitions.cs', 'Runtime/Domain/CityProject.cs']:
    edit(p, 'public string[] workerRewards = Array.Empty<string>();', 'public string[] workerRewards = Array.Empty<string>();\n        public ItemStack[] itemRewards = Array.Empty<ItemStack>();')
edit('Runtime/Domain/Definitions.cs', 'public CityProjectDefinition cityProject;', 'public CityProjectDefinition cityProject;\n        public ItemDefinition[] items = Array.Empty<ItemDefinition>();\n        public ItemStack[] vipItemRewards = Array.Empty<ItemStack>();')
edit('Runtime/Domain/Definitions.cs', 'cityProject?.Validate(this);', 'InventoryItems.ValidateDefinitions(this);\n            InventoryItems.ValidateGrants(this, vipItemRewards);\n            cityProject?.Validate(this);')
edit('Runtime/Domain/Definitions.cs', 'foreach (var role in q.workerRewards) Role(role);', 'foreach (var role in q.workerRewards) Role(role);\n                InventoryItems.ValidateGrants(this, q.itemRewards);')
edit('Runtime/Domain/CityProject.cs', 'foreach (string role in step.workerRewards) defs.Role(role);', 'foreach (string role in step.workerRewards) defs.Role(role);\n                    InventoryItems.ValidateGrants(defs, step.itemRewards);')
edit('Runtime/Domain/Simulation.cs', '''if (b.buildingSeconds > 0 && --b.buildingSeconds == 0)
                {
                    if (b.level == 0) { b.lastDailyDay = state.highWaterUtcTicks / TimeSpan.TicksPerDay - 1; b.dailyTier = 1; }
                    b.level++; state.revision++;
                }''', 'ConstructionProgress.Reduce(state, b, 1);')
edit('Runtime/Domain/UpgradeService.cs', '''if(b.level==0){b.lastDailyDay=state.highWaterUtcTicks/TimeSpan.TicksPerDay-1;b.dailyTier=1;}
                b.buildingSeconds = 0; b.level++; state.revision++;''', 'ConstructionProgress.Reduce(state, b, b.buildingSeconds);')
edit('Runtime/Application/GameSession.cs', 'public CityProjectService CityProject { get; }', 'public CityProjectService CityProject { get; }\n        public InventoryService Inventory { get; }')
edit('Runtime/Application/GameSession.cs', 'CityProject = new CityProjectService(defs, state, Simulation);', 'CityProject = new CityProjectService(defs, state, Simulation);\n            Inventory = new InventoryService(defs, state, Workers);')
edit('Runtime/Application/ProgressionService.cs', 'var rewards = RewardHistory.Currency(q.moneyReward, q.cardsReward);', 'var rewards = RewardHistory.Currency(q.moneyReward, q.cardsReward);\n            InventoryItems.Grant(state, q.itemRewards, rewards);')
edit('Runtime/Application/ProgressionService.cs', 'RewardHistory.Record(state, RewardSource.Vip, q.id, "reward.source.vip", RewardHistory.Currency(q.reward));', 'var rewards = RewardHistory.Currency(q.reward);\n            InventoryItems.Grant(state, defs.vipItemRewards, rewards);\n            RewardHistory.Record(state, RewardSource.Vip, q.id, "reward.source.vip", rewards);')
edit('Runtime/Application/CityProjectService.cs', 'var rewards = RewardHistory.Currency(step.moneyReward, step.cardsReward);', 'var rewards = RewardHistory.Currency(step.moneyReward, step.cardsReward);\n            InventoryItems.Grant(state, step.itemRewards, rewards);')
edit('Runtime/Application/RewardCatalog.cs', 'if (quest != null)', 'foreach (var stack in quest?.itemRewards ?? step?.itemRewards ?? System.Array.Empty<ItemStack>())\n                rewards.Add(RewardItem.Of(RewardKind.Item, stack.count, stack.id));\n            if (quest != null)')
edit('Runtime/Application/RewardCatalog.cs', '''var result = new List<RewardItem> { RewardItem.Of(RewardKind.Money, state.money),
                RewardItem.Of(RewardKind.Grinders, state.grinders), RewardItem.Of(RewardKind.Cards, state.cards) };''', '''var result = new List<RewardItem>();
            if (state.cards > 0) result.Add(RewardItem.Of(RewardKind.Cards, state.cards));
            result.AddRange((state.inventory ?? new List<ItemStack>()).Where(s => s.count > 0).Select(s => RewardItem.Of(RewardKind.Item, s.count, s.id)));''')
edit('Runtime/Persistence/RewardSaveValidator.cs', 'Require(state.rewardSequence >= 0', '''if (state.inventory != null)
            {
                Require(state.inventory.All(s => s != null && s.count > 0 && (defs.items ?? Array.Empty<ItemDefinition>()).Any(i => i.id == s.id)));
                Require(state.inventory.Select(s => s.id).Distinct().Count() == state.inventory.Count);
            }
            Require(state.rewardSequence >= 0''')
edit('Runtime/Persistence/RewardSaveValidator.cs', 'if (item.kind == RewardKind.Worker)', '''if (item.kind == RewardKind.Item)
                        Require((defs.items ?? Array.Empty<ItemDefinition>()).Any(i => i.id == item.key) && item.quantity <= int.MaxValue &&
                            item.quantity.ToDouble() == Math.Floor(item.quantity.ToDouble()));
                    if (item.kind == RewardKind.Worker)''')
edit('Runtime/Unity/RewardText.cs', 'case RewardKind.Worker:\n                    string key', 'case RewardKind.Item:\n                    var definition = System.Array.Find(defs.items, i => i.id == item.key);\n                    return defs.Text(compact ? definition.shortNameKey : definition.nameKey);\n                case RewardKind.Worker:\n                    string key')
edit('Runtime/Unity/RewardText.cs', 'if (item.kind == RewardKind.Worker)', '''if (item.kind == RewardKind.Item)
            {
                var definition = System.Array.Find(defs.items, i => i.id == item.key);
                return defs.Text("inventory.category." + definition.category);
            }
            if (item.kind == RewardKind.Worker)''')
edit('Runtime/Unity/RewardText.cs', 'public static string Icon(RewardItem item)', 'public static string Icon(RewardItem item, GameDefinitions defs = null)')
edit('Runtime/Unity/RewardText.cs', 'case RewardKind.Money: return "money";', 'case RewardKind.Item: return defs == null ? "gift" : System.Array.Find(defs.items, i => i.id == item.key).icon;\n                case RewardKind.Money: return "money";')
edit('Runtime/Unity/RewardCardView.cs', 'RewardText.Icon(item)', 'RewardText.Icon(item, root.Definitions)')
edit('Runtime/Unity/RewardPreviewView.cs', 'RewardText.Icon(list[i])', 'RewardText.Icon(list[i], root.Definitions)')
# Every received reward remains viewable, including future receipts longer than six entries.
edit('Runtime/Unity/RewardPresentation.cs', 'private int visibleCount;', 'private int visibleCount, itemOffset;')
edit('Runtime/Unity/RewardPresentation.cs', 'Current = pending.Dequeue(); started = Time.unscaledTime; revealed = false;', 'Current = pending.Dequeue(); itemOffset = 0; PaintPage();\n        }\n        private void PaintPage()\n        {\n            started = Time.unscaledTime; revealed = false;')
edit('Runtime/Unity/RewardPresentation.cs', 'counter.text = RewardText.Source(root.Definitions, Current);', 'counter.text = RewardText.Source(root.Definitions, Current) + (Current.items.Count > cards.Length ? " · " + (itemOffset / cards.Length + 1) + " / " + Mathf.CeilToInt((float)Current.items.Count / cards.Length) : "");')
edit('Runtime/Unity/RewardPresentation.cs', 'Mathf.Min(cards.Length, Current.items.Count)', 'Mathf.Min(cards.Length, Current.items.Count - itemOffset)')
edit('Runtime/Unity/RewardPresentation.cs', 'Current.items[i]', 'Current.items[itemOffset + i]')
edit('Runtime/Unity/RewardPresentation.cs', 'if (pending.Count > 0) Next(); else DismissAll();', 'if (itemOffset + visibleCount < Current.items.Count) { itemOffset += visibleCount; PaintPage(); }\n            else if (pending.Count > 0) Next(); else DismissAll();')
print('Inventory services integrated.')
