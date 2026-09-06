using UnityEngine;
using UnityEngine.Localization;

namespace SortThem
{
    public enum UpgradeKind
    {
        Inventory,
        Range,
        Sprint,
        Crouch,
        ThrowPower,
        AutoPlace,
        ShelfHighlight,
        DuplicateHighlight,
        AutoCollect
    }

    [CreateAssetMenu(menuName = "SortThem/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string UpgradeID;
        public string DevName;
        public string DevDescription;
        public UpgradeKind Kind;
        public LocalizedString DisplayName;
        public LocalizedString Description;
        public Sprite Icon;
        public int[] CostPerLevel = { 100 };
        public float[] ValuePerLevel = { 1f };

        public int MaxLevel => CostPerLevel != null ? CostPerLevel.Length : 0;

        public int CostOf(int level)
        {
            if (CostPerLevel == null || level < 0 || level >= CostPerLevel.Length) return 0;
            return CostPerLevel[level];
        }

        public float ValueAt(int level, float fallback)
        {
            if (level <= 0 || ValuePerLevel == null || ValuePerLevel.Length == 0) return fallback;
            int i = Mathf.Clamp(level - 1, 0, ValuePerLevel.Length - 1);
            return ValuePerLevel[i];
        }
    }
}
