using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class UpgradeSetup
    {
        struct Def
        {
            public string Id, Dev, DevDesc, Ru, RuDesc, En, EnDesc;
            public UpgradeKind Kind;
            public UpgradeSource Source;
            public int[] Costs;
            public float[] Values;
        }

        static int[] Seq(int first, int step, int count) { var a = new int[count]; for (int i = 0; i < count; i++) a[i] = first + step * i; return a; }
        static float[] SeqF(float first, float step, int count) { var a = new float[count]; for (int i = 0; i < count; i++) a[i] = first + step * i; return a; }

        static readonly Def[] Defs =
        {
            new Def { Id = "upg_inventory", Kind = UpgradeKind.Inventory, Dev = "Bigger inventory", DevDesc = "+1 slot per level, 10 levels, max 15 (slot machine adds up to 20)", Ru = "Больший инвентарь", RuDesc = "+1 слот за уровень, максимум 15", En = "Bigger inventory", EnDesc = "+1 slot per level, up to 15", Costs = Seq(30, 10, 10), Values = SeqF(1f, 1f, 10) },
            new Def { Id = "upg_range", Kind = UpgradeKind.Range, Dev = "Longer reach", DevDesc = "Interaction range x1.25/1.5/1.8/2.2", Ru = "Большая дальность", RuDesc = "Брать и ставить машинки с большего расстояния", En = "Longer reach", EnDesc = "Take and place cars from farther away", Costs = new[] { 20, 30, 40, 50 }, Values = new[] { 1.25f, 1.5f, 1.8f, 2.2f } },
            new Def { Id = "upg_sprint", Kind = UpgradeKind.Sprint, Dev = "Unlock sprint", DevDesc = "Shift to run", Ru = "Открыть спринт", RuDesc = "Бег на Shift", En = "Unlock sprint", EnDesc = "Hold Shift to run", Costs = new[] { 50 }, Values = new[] { 1f } },
            new Def { Id = "upg_crouch", Kind = UpgradeKind.Crouch, Dev = "Unlock crouch", DevDesc = "Ctrl to crouch under shelves", Ru = "Открыть присед", RuDesc = "Присед на Ctrl, чтобы собирать под полками", En = "Unlock crouch", EnDesc = "Hold Ctrl to crouch under shelves", Costs = new[] { 30 }, Values = new[] { 1f } },
            new Def { Id = "upg_throw", Kind = UpgradeKind.ThrowPower, Dev = "Stronger throw", DevDesc = "Throw distance x1.5/2.0/2.5", Ru = "Более сильный бросок", RuDesc = "Машинки летят дальше", En = "Stronger throw", EnDesc = "Cars fly farther", Costs = new[] { 20, 50, 70 }, Values = new[] { 1.5f, 2f, 2.5f } },
            new Def { Id = "upg_autoplace", Kind = UpgradeKind.AutoPlace, Dev = "Auto-place on throw", DevDesc = "Thrown car snaps into its category rack", Ru = "Авторасстановка при броске", RuDesc = "Брошенная в свой стеллаж машинка сама встаёт на полку", En = "Auto-place on throw", EnDesc = "A car thrown into its rack snaps onto a shelf", Costs = new[] { 100 }, Values = new[] { 1f } },
            new Def { Id = "upg_dup_highlight", Kind = UpgradeKind.DuplicateHighlight, Dev = "Find matches", DevDesc = "Key 1: matching cars in the pile glow purple and float up", Ru = "Поиск совпадений", RuDesc = "Клавиша 1: такие же машинки в куче подсвечиваются и поднимаются в воздух", En = "Find matches", EnDesc = "Key 1: matching cars in the pile glow and float up", Costs = new[] { 100 }, Values = new[] { 1f } },
            new Def { Id = "upg_autocollect", Kind = UpgradeKind.AutoCollect, Dev = "Auto-collect", DevDesc = "Key 2: pulls same cars nearby into inventory", Ru = "Автосбор совпадений", RuDesc = "Клавиша 2: затягивает такие же машинки поблизости в инвентарь", En = "Auto-collect", EnDesc = "Key 2: pulls the same cars nearby into your inventory", Costs = new[] { 150 }, Values = new[] { 1f } },
            new Def { Id = "upg_shelf_highlight", Kind = UpgradeKind.ShelfHighlight, Dev = "Rack highlight", DevDesc = "Key 3: highlights the rack of the held car's category", Ru = "Подсветка стеллажа", RuDesc = "Клавиша 3: подсвечивает стеллаж категории машинки в руках", En = "Rack highlight", EnDesc = "Key 3: highlights the rack for the car in hand", Costs = new[] { 100 }, Values = new[] { 1f } },
            new Def { Id = "slot_inventory", Kind = UpgradeKind.InventoryOverCap, Source = UpgradeSource.Slot, Dev = "Extra slot", DevDesc = "Slot machine: +1 inventory slot per level, 15 -> 20", Ru = "Ещё один слот", RuDesc = "Инвентарь +1 сверх терминала", En = "Extra slot", EnDesc = "+1 inventory slot beyond the terminal", Costs = new[] { 0, 0, 0, 0, 0 }, Values = SeqF(1f, 1f, 5) },
            new Def { Id = "slot_cooldown", Kind = UpgradeKind.AbilityCooldown, Source = UpgradeSource.Slot, Dev = "Faster cooldown", DevDesc = "Slot machine: ability cooldown x0.9/0.8/0.7", Ru = "Быстрый откат", RuDesc = "Способности откатываются быстрее", En = "Faster cooldown", EnDesc = "Abilities recharge faster", Costs = new[] { 0, 0, 0 }, Values = new[] { 0.9f, 0.8f, 0.7f } },
            new Def { Id = "slot_radius", Kind = UpgradeKind.AutoCollectRadius, Source = UpgradeSource.Slot, Dev = "Wider auto-collect", DevDesc = "Slot machine: auto-collect radius x1.25/1.5/1.75 (8 -> 10/12/14 m)", Ru = "Широкий автосбор", RuDesc = "Автосбор тянет машинки с большего расстояния", En = "Wider auto-collect", EnDesc = "Auto-collect reaches farther", Costs = new[] { 0, 0, 0 }, Values = new[] { 1.25f, 1.5f, 1.75f } },
            new Def { Id = "slot_duration", Kind = UpgradeKind.AbilityDuration, Source = UpgradeSource.Slot, Dev = "Longer effect", DevDesc = "Slot machine: find matches and rack highlight last x1.333/1.667/2 (30 -> 40/50/60 s)", Ru = "Долгое действие", RuDesc = "Поиск совпадений и подсветка стеллажа держатся дольше", En = "Longer effect", EnDesc = "Find matches and rack highlight last longer", Costs = new[] { 0, 0, 0 }, Values = new[] { 4f / 3f, 5f / 3f, 2f } },
        };

        [MenuItem("SortThem/3b. Create Upgrades")]
        public static void Menu() => CreateAll(false);

        [MenuItem("SortThem/3c. Reset Upgrade Prices To Reference")]
        public static void ResetPrices() => CreateAll(true);

        public static UpgradeData[] CreateAll(bool resetBalance)
        {
            EditorAssets.EnsureFolder(Paths.Upgrades);
            var list = new List<UpgradeData>();
            foreach (var d in Defs)
            {
                string path = Paths.Upgrades + "/" + d.Id + ".asset";
                bool isNew = AssetDatabase.LoadAssetAtPath<UpgradeData>(path) == null;
                var u = EditorAssets.LoadOrCreate<UpgradeData>(path);
                u.UpgradeID = d.Id;
                u.Kind = d.Kind;
                u.Source = d.Source;
                u.DevName = d.Dev;
                u.DevDescription = d.DevDesc;
                if (isNew || resetBalance)
                {
                    u.CostPerLevel = d.Costs;
                    u.ValuePerLevel = d.Values;
                }
                LocUtil.Set(d.Id + ".name", d.Ru, d.En);
                LocUtil.Set(d.Id + ".desc", d.RuDesc, d.EnDesc);
                u.DisplayName = LocUtil.Ref(d.Id + ".name");
                u.Description = LocUtil.Ref(d.Id + ".desc");
                EditorUtility.SetDirty(u);
                list.Add(u);
            }
            AssetDatabase.SaveAssets();
            return list.ToArray();
        }
    }
}
