using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SortThem.Editor
{
    public static class FontAtlasTool
    {
        const int PointSize = 86;
        const int Padding = 9;
        const int CjkPointSize = 64;
        const int CjkPadding = 7;
        public static string LastReport;
        const GlyphRenderMode RenderMode = GlyphRenderMode.SDFAA;
        const string BaseRanges = "32 - 126, 160 - 255, 8192 - 8303, 8364, 8482, 9633";
        const string CharsDir = "Tools/loc/chars";
        const string OutDir = FontAddressables.Dir;
        const string LiberationAsset = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static readonly (string Code, string Ttf, string Name)[] Cjk =
        {
            ("ja", "Assets/_Game/Fonts/NotoSansJP-Regular.ttf", "NotoSansJP SDF"),
            ("ko", "Assets/_Game/Fonts/NotoSansKR-Regular.ttf", "NotoSansKR SDF"),
            ("zh", "Assets/_Game/Fonts/NotoSansSC-Regular.ttf", "NotoSansSC SDF"),
            ("zh-Hant", "Assets/_Game/Fonts/NotoSansTC-Regular.ttf", "NotoSansTC SDF"),
        };

        [MenuItem("SortThem/Loc/Fonts/Build CJK Atlases (static)")]
        public static void BuildCjk()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("FontAtlasTool: останови Play."); return; }
            Directory.CreateDirectory(OutDir);
            var report = new StringBuilder("FontAtlasTool CJK:\n");
            foreach (var (code, ttf, name) in Cjk)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                if (font == null) { report.Append(code).Append(": TTF не импортирован ").Append(ttf).Append('\n'); continue; }
                string chars = Chars(code);
                string path = OutDir + "/" + name + ".asset";
                report.Append(BuildStatic(font, chars, path, name, CjkPointSize, CjkPadding)).Append('\n');
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            FontAddressables.Ensure();
            LastReport = report.ToString();
            Debug.Log(LastReport);
        }

        [MenuItem("SortThem/Loc/Fonts/Rebuild LiberationSans Atlas (static, Latin+Cyr)")]
        public static void RebuildLiberation()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("FontAtlasTool: останови Play."); return; }
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationAsset);
            if (fa == null) { Debug.LogError("FontAtlasTool: не найден " + LiberationAsset); return; }
            string chars = Chars("_latin_cyr_all");
            var refField = typeof(TMP_FontAsset).GetField("m_SourceFontFile_EditorRef", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var srcFont = refField?.GetValue(fa) as Font;
            if (srcFont == null) srcFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(fa.creationSettings.sourceFontFileGUID));
            if (srcFont == null) { Debug.LogError("FontAtlasTool: у LiberationSans SDF нет ссылки на исходный TTF."); return; }
            refField?.SetValue(fa, srcFont);
            var missingInFont = MissingInFont(srcFont, chars);
            if (missingInFont.Length > 0) Debug.LogWarning("LiberationSans: в шрифте нет символов: " + missingInFont);

            string stamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "backups");
            Directory.CreateDirectory(backupDir);
            File.Copy(LiberationAsset, Path.Combine(backupDir, "LiberationSans_SDF_" + stamp + ".asset"), true);

            var tex = fa.atlasTextures[0];
            if (!tex.isReadable) SetReadable(tex);
            fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fa.ClearFontAssetData(true);
            string result = null;
            foreach (int size in new[] { 1024, 2048 })
            {
                SetAtlasSize(fa, size);
                fa.ClearFontAssetData(true);
                bool ok = fa.TryAddCharacters(chars, out string missing);
                string unfit = Strip(missing, missingInFont);
                if (ok || unfit.Length == 0) { result = "LiberationSans SDF: " + fa.characterTable.Count + " символов, атлас " + size + "²"; break; }
                result = "LiberationSans SDF: не влезло в " + size + "²: " + unfit;
            }
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            fa.atlasTextures[0].Apply(false, true);
            var cs = fa.creationSettings;
            cs.pointSize = PointSize; cs.padding = Padding; cs.atlasWidth = fa.atlasWidth; cs.atlasHeight = fa.atlasHeight;
            cs.characterSetSelectionMode = 7; cs.characterSequence = chars; cs.renderMode = (int)RenderMode;
            fa.creationSettings = cs;
            fa.fallbackFontAssetTable.Clear();
            EditorUtility.SetDirty(fa);
            EditorUtility.SetDirty(fa.atlasTextures[0]);
            AssetDatabase.SaveAssets();
            LastReport = result + "; бэкап " + backupDir;
            Debug.Log(LastReport);
        }

        static string BuildStatic(Font font, string chars, string path, string name, int pointSize, int padding)
        {
            var missingInFont = MissingInFont(font, chars);
            foreach (int size in new[] { 1024, 2048 })
            {
                var fa = TMP_FontAsset.CreateFontAsset(font, pointSize, padding, RenderMode, size, size, AtlasPopulationMode.Dynamic, false);
                bool ok = fa.TryAddCharacters(chars, out string missing);
                string unfit = Strip(missing, missingInFont);
                if (!ok && unfit.Length > 0) { Object.DestroyImmediate(fa.material); Object.DestroyImmediate(fa.atlasTextures[0]); Object.DestroyImmediate(fa); continue; }

                fa.atlasPopulationMode = AtlasPopulationMode.Static;
                fa.name = name;
                var tex = fa.atlasTextures[0];
                tex.name = name + " Atlas";
                tex.Apply(false, true);
                var mat = fa.material;
                mat.name = name + " Material";
                var cs = fa.creationSettings;
                cs.sourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font));
                cs.pointSizeSamplingMode = 0; cs.pointSize = pointSize; cs.padding = padding; cs.packingMode = 4;
                cs.atlasWidth = size; cs.atlasHeight = size; cs.characterSetSelectionMode = 7; cs.characterSequence = chars;
                cs.renderMode = (int)RenderMode; cs.includeFontFeatures = true;
                fa.creationSettings = cs;

                var old = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (old != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(fa, path);
                AssetDatabase.AddObjectToAsset(tex, fa);
                AssetDatabase.AddObjectToAsset(mat, fa);
                EditorUtility.SetDirty(fa);
                string note = missingInFont.Length > 0 ? "; нет в шрифте: " + missingInFont : "";
                return name + ": " + fa.characterTable.Count + " символов, атлас " + size + "²" + note;
            }
            return name + ": не влезло в 2048² при " + pointSize + "pt";
        }

        static string Chars(string code)
        {
            var set = new System.Collections.Generic.SortedSet<int>();
            foreach (var part in BaseRanges.Split(','))
            {
                var r = part.Split('-');
                int a = int.Parse(r[0].Trim());
                int b = r.Length > 1 ? int.Parse(r[1].Trim()) : a;
                for (int c = a; c <= b; c++) set.Add(c);
            }
            string file = Path.Combine(Directory.GetCurrentDirectory(), CharsDir, code + ".txt");
            if (!File.Exists(file)) throw new FileNotFoundException("Нет набора символов: " + file + " (сначала выгрузи из таблиц)");
            string s = File.ReadAllText(file);
            for (int i = 0; i < s.Length; i++)
            {
                int cp = char.ConvertToUtf32(s, i);
                if (char.IsHighSurrogate(s[i])) i++;
                if (cp < 32) continue;
                set.Add(cp);
                if (cp <= 0xFFFF)
                {
                    set.Add(char.ToUpperInvariant((char)cp));
                    set.Add(char.ToLowerInvariant((char)cp));
                }
            }
            var sb = new StringBuilder();
            foreach (var cp in set) sb.Append(char.ConvertFromUtf32(cp));
            return sb.ToString();
        }

        static string MissingInFont(Font font, string chars)
        {
            if (font == null || FontEngine.LoadFontFace(font, PointSize) != FontEngineError.Success) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                int cp = char.ConvertToUtf32(chars, i);
                if (char.IsHighSurrogate(chars[i])) i++;
                if (!FontEngine.TryGetGlyphIndex((uint)cp, out uint idx) || idx == 0) sb.Append(char.ConvertFromUtf32(cp));
            }
            return sb.ToString();
        }

        static void SetReadable(Texture2D tex)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("UnityEditor.TextCore.LowLevel.FontEngineEditorUtilities");
                if (t == null) continue;
                var m = t.GetMethod("SetAtlasTextureIsReadable", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (m == null) continue;
                m.Invoke(null, new object[] { tex, true });
                return;
            }
            throw new System.InvalidOperationException("FontEngineEditorUtilities.SetAtlasTextureIsReadable не найден");
        }

        static void SetAtlasSize(TMP_FontAsset fa, int size)
        {
            var so = new SerializedObject(fa);
            so.FindProperty("m_AtlasWidth").intValue = size;
            so.FindProperty("m_AtlasHeight").intValue = size;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static string Strip(string missing, string missingInFont)
        {
            if (string.IsNullOrEmpty(missing)) return "";
            var sb = new StringBuilder();
            foreach (var ch in missing) if (missingInFont.IndexOf(ch) < 0) sb.Append(ch);
            return sb.ToString();
        }
    }
}
