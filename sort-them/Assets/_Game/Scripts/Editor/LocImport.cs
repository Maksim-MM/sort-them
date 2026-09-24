using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SortThem.Editor
{
    public static class LocImport
    {
        const string OutDir = "Tools/loc/out";
        const string ExportPath = "Tools/loc/export.tsv";

        [MenuItem("SortThem/Loc/Import Translations (Tools/loc/out)")]
        public static void Import()
        {
            var coll = LocalizationEditorSettings.GetStringTableCollection(Loc.Table);
            if (coll == null) { Debug.LogError("LocImport: no collection " + Loc.Table); return; }
            var report = new StringBuilder("LocImport: ");
            foreach (var file in Directory.GetFiles(OutDir, "*.json"))
            {
                string code = Path.GetFileNameWithoutExtension(file);
                var table = coll.GetTable(new LocaleIdentifier(code)) as StringTable;
                if (table == null) { report.Append(code + ": NO TABLE; "); continue; }
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                int set = 0, unknown = 0;
                foreach (var kv in data)
                {
                    if (coll.SharedData.GetEntry(kv.Key) == null) { unknown++; continue; }
                    table.AddEntry(kv.Key, kv.Value);
                    set++;
                }
                EditorUtility.SetDirty(table);
                report.Append($"{code}: set {set}, unknown {unknown}; ");
            }
            EditorUtility.SetDirty(coll.SharedData);
            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        [MenuItem("SortThem/Loc/Export All Tables (Tools/loc/export.tsv)")]
        public static void Export()
        {
            var coll = LocalizationEditorSettings.GetStringTableCollection(Loc.Table);
            if (coll == null) return;
            var tables = new List<StringTable>();
            foreach (var t in coll.StringTables) tables.Add(t);
            tables.Sort((a, b) => string.CompareOrdinal(a.LocaleIdentifier.Code, b.LocaleIdentifier.Code));
            var sb = new StringBuilder("key");
            foreach (var t in tables) sb.Append('\t').Append(t.LocaleIdentifier.Code);
            sb.Append('\n');
            var keys = new List<string>();
            foreach (var e in coll.SharedData.Entries) keys.Add(e.Key);
            keys.Sort(string.CompareOrdinal);
            foreach (var key in keys)
            {
                sb.Append(key);
                foreach (var t in tables)
                {
                    var e = t.GetEntry(key);
                    sb.Append('\t').Append(e != null ? (e.Value ?? "").Replace("\n", "\\n").Replace("\t", " ") : "");
                }
                sb.Append('\n');
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ExportPath));
            File.WriteAllText(ExportPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"LocImport: exported {keys.Count} keys x {tables.Count} locales to {ExportPath}");
        }
    }
}
