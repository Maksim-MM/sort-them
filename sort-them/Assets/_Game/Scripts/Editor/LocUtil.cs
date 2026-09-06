using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SortThem.Editor
{
    public static class LocUtil
    {
        public const string TableName = "Game";

        public static StringTableCollection Collection => LocalizationEditorSettings.GetStringTableCollection(TableName);

        public static LocalizedString Ref(string key)
        {
            var coll = Collection;
            if (coll == null) return new LocalizedString();
            return new LocalizedString(coll.SharedData.TableCollectionNameGuid, key);
        }

        public static void Set(string key, string ru, string en)
        {
            var coll = Collection;
            if (coll == null) return;
            var shared = coll.SharedData;
            if (shared.GetEntry(key) == null) shared.AddKey(key);
            foreach (var table in coll.StringTables)
            {
                string value = table.LocaleIdentifier.Code.StartsWith("ru") ? ru : en;
                table.AddEntry(key, value);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(shared);
        }
    }
}
