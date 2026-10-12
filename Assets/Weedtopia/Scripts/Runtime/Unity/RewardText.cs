using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public static class RewardText
    {
        public static string Name(GameDefinitions defs, RewardItem item, bool compact = false)
        {
            switch (item.kind)
            {
                case RewardKind.StoryMemory: return defs.Text(item.key);
                case RewardKind.Item:
                    var definition = System.Array.Find(defs.items, i => i.id == item.key);
                    return defs.Text(compact ? definition.shortNameKey : definition.nameKey);
                case RewardKind.Worker:
                    string key = "reward.role." + item.key + (compact ? ".compact" : "");
                    foreach (var text in defs.texts) if (text.key == key) return defs.Text(key);
                    return defs.Text(defs.Role(item.key).nameKey);
                case RewardKind.District: return defs.Text("zone." + item.key);
                case RewardKind.Title: return defs.Text("reward.title." + item.key + (compact && item.key=="founder" ? ".compact" : ""));
                case RewardKind.SalesBonus: return defs.Text(defs.Building(item.key).nameKey);
                default: return defs.Text("reward.kind." + item.kind + (compact && item.kind==RewardKind.Cards ? ".compact" : ""));
            }
        }
        public static string Quantity(RewardItem item, bool inventory = false)
        {
            switch (item.kind)
            {
                case RewardKind.Title: case RewardKind.District: case RewardKind.StoryMemory: return "";
                case RewardKind.SalesBonus: return "+" + item.quantity + " %";
                case RewardKind.Worker: return "";
                default: return (inventory ? "" : "+") + item.quantity;
            }
        }
        public static string Detail(GameRoot root, RewardItem item, bool inventory)
        {
            var defs = root.Definitions;
            if (item.kind == RewardKind.StoryMemory) return defs.Text("story.memory_detail");
            if (item.kind == RewardKind.Item)
            {
                var definition = System.Array.Find(defs.items, i => i.id == item.key);
                return defs.Text("inventory.category." + definition.category);
            }
            if (item.kind == RewardKind.Worker)
            {
                var worker = root.Session.State.workers.Find(w => w.id == item.reference);
                return defs.Text("rarity." + item.rarity) + (inventory && worker != null ? " · " + defs.Text(string.IsNullOrEmpty(worker.buildingId) ? "ui.unassigned" : "reward.assigned") : " · " + defs.Text("reward.kind.Worker"));
            }
            return defs.Text(item.kind == RewardKind.Title ? "reward.permanent" : item.kind == RewardKind.SalesBonus ? "reward.sales_bonus" :
                item.kind == RewardKind.District ? "reward.unlocked" : item.kind == RewardKind.VipOrders ? "reward.orders_hint" : inventory ? "reward.available" : "reward.credited");
        }
        public static string Icon(RewardItem item, GameDefinitions defs = null)
        {
            switch (item.kind)
            {
                case RewardKind.Item: return defs == null ? "gift" : System.Array.Find(defs.items, i => i.id == item.key).icon;
                case RewardKind.Money: return "money";
                case RewardKind.Grinders: return "grinder";
                case RewardKind.Cards: return "cards";
                case RewardKind.Worker: return "portrait_" + PresentationArt.CharacterForRole(item.key);
                case RewardKind.Title: return "laurel";
                case RewardKind.StoryMemory: return "laurel";
                case RewardKind.SalesBonus: return "star";
                case RewardKind.District: return item.key == "Industry" ? "industry" : item.key == "City" ? "city" : "settlement";
                default: return "quest";
            }
        }
        public static string Source(GameDefinitions defs, RewardReceipt receipt) => defs.Text("reward.source." + receipt.source.ToString().ToLowerInvariant());
    }
}
