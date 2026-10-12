using System;
using System.Collections.Generic;
using System.Linq;

namespace Weedtopia.Domain
{
    public enum ItemCategory { Workers, Boosters, Supplies }
    public enum ItemEffect { ConstructionTime, Cards, WorkerChoice, Money, Grinders }

    [Serializable] public sealed class ItemDefinition
    {
        public string id, nameKey, shortNameKey, descriptionKey, icon;
        public ItemCategory category;
        public ItemEffect effect;
        public int value, rarity;
    }
    [Serializable] public sealed class ItemStack { public string id; public int count; }

    /// <summary>Balances and grants are independent of the Unity inventory view and reward history.</summary>
    public static class InventoryItems
    {
        public static int Count(GameState state, string id) => state.inventory?.FirstOrDefault(x => x.id == id)?.count ?? 0;
        public static void Grant(GameState state, IEnumerable<ItemStack> grants, List<RewardItem> receipt)
        {
            foreach (var grant in grants ?? Array.Empty<ItemStack>())
            {
                state.inventory ??= new List<ItemStack>();
                var stack = state.inventory.FirstOrDefault(x => x.id == grant.id);
                if (stack == null) { stack = new ItemStack { id = grant.id }; state.inventory.Add(stack); }
                stack.count = checked(stack.count + grant.count);
                receipt.Add(RewardItem.Of(RewardKind.Item, grant.count, grant.id));
            }
        }
        public static void ValidateDefinitions(GameDefinitions defs)
        {
            var items = defs.items ?? Array.Empty<ItemDefinition>();
            if (items.Any(i => i == null || string.IsNullOrWhiteSpace(i.id)) || items.Select(i => i.id).Distinct().Count() != items.Length)
                throw new InvalidOperationException("Missing or duplicate item definition");
            foreach (var item in items)
            {
                bool category = item.category == (item.effect == ItemEffect.ConstructionTime ? ItemCategory.Boosters :
                    item.effect == ItemEffect.Cards || item.effect == ItemEffect.WorkerChoice ? ItemCategory.Workers : ItemCategory.Supplies);
                if (!category || !Enum.IsDefined(typeof(ItemEffect), item.effect) || item.value < 1 || item.rarity < 0 || item.rarity > 4 ||
                    (item.effect == ItemEffect.ConstructionTime && item.value > 86400) ||
                    (item.effect == ItemEffect.WorkerChoice && item.value != 1) || string.IsNullOrWhiteSpace(item.icon) ||
                    new[] { item.nameKey, item.shortNameKey, item.descriptionKey }.Any(k => !defs.texts.Any(t => t.key == k)))
                    throw new InvalidOperationException("Invalid inventory item " + item.id);
            }
        }
        public static void ValidateGrants(GameDefinitions defs, ItemStack[] grants)
        {
            if (grants == null) return; // Optional for older content and saves.
            if (grants.Any(g => g == null || g.count < 1 || g.count > 1000 || !(defs.items ?? Array.Empty<ItemDefinition>()).Any(i => i.id == g.id)) ||
                grants.Select(g => g.id).Distinct().Count() != grants.Length)
                throw new InvalidOperationException("Invalid item rewards");
        }
    }

    /// <summary>Shared completion path for ticking, premium finish and inventory speedups.</summary>
    public static class ConstructionProgress
    {
        public static void Reduce(GameState state, BuildingState building, int seconds)
        {
            if (seconds <= 0 || building.buildingSeconds <= 0) return;
            building.buildingSeconds = Math.Max(0, building.buildingSeconds - seconds);
            if (building.buildingSeconds != 0) return;
            if (building.level == 0)
            {
                building.lastDailyDay = state.highWaterUtcTicks / TimeSpan.TicksPerDay - 1;
                building.dailyTier = 1;
            }
            building.level++; state.revision++;
        }
    }
}
