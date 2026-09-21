using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class AudioSetup
    {
        const string Folder = "Assets/_Game/Audio";
        const string Footsteps = Folder + "/Footsteps";
        const string Music = Folder + "/Music";

        static readonly (string field, string file)[] Singles =
        {
            ("PickupClip", "pickup_car"), ("PlaceClip", "place_car"), ("ThrowClip", "throw_car"), ("BounceClip", "bounce_car"),
            ("ShelfCompleteClip", "shelf_complete"), ("CollectibleClip", "collectible_pickup"), ("SpecialUpgradeClip", "special_upgrade"),
            ("AbilityClip", "ability"), ("AbilityReadyClip", "ability_ready"), ("AbilityNotReadyClip", "ability_not_ready"),
            ("PurchaseClip", "purchase"), ("SlotLeverClip", "slot_lever"), ("SlotReelClip", "slot_reel"), ("SlotSpinClip", "slot_spin"), ("SlotWinClip", "slot_win"),
            ("RegisterClickClip", "register_click"), ("RegisterPayClip", "register_pay"), ("UiMoveClip", "ui_move"), ("UiClickClip", "ui_click"),
            ("FootstepWalkClip", "footstep_walk"), ("FootstepRunClip", "footstep_run"), ("BombFuseClip", "bomb_fuse"), ("BombExplodeClip", "bomb_explode")
        };

        [MenuItem("SortThem/7. Assign Audio")]
        public static void Assign()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(Paths.Config + "/GameConfig.asset");
            if (cfg == null) { Debug.LogError("SortThem: GameConfig not found"); return; }
            var log = new System.Text.StringBuilder();
            int assigned = 0, missing = 0;
            foreach (var (field, file) in Singles)
            {
                var clip = Load(Folder, file);
                var f = typeof(GameConfig).GetField(field);
                if (clip != null) { f.SetValue(cfg, clip); assigned++; }
                else if (f.GetValue(cfg) == null) { missing++; log.Append(field + " ");}
                Tune(clip, false);
            }
            cfg.FootstepWalkClips = LoadAll(Footsteps, "footstep_walk_");
            cfg.FootstepRunClips = LoadAll(Footsteps, "footstep_run_");
            cfg.AbilityClips = LoadAll(Folder, "ability_");
            foreach (var c in cfg.FootstepWalkClips.Concat(cfg.FootstepRunClips).Concat(cfg.AbilityClips)) Tune(c, false);
            var music = LoadAll(Music, "music_");
            if (music.Length > 0) cfg.MusicClips = music;
            foreach (var c in cfg.MusicClips) Tune(c, true);
            if (cfg.SlotSpinClip != null) cfg.SlotReelClip = null;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            Debug.Log($"SortThem audio: {assigned} slots assigned, {cfg.FootstepWalkClips.Length} walk steps, {cfg.FootstepRunClips.Length} run steps, {cfg.AbilityClips.Length} ability variants, {cfg.MusicClips.Length} music tracks; still empty: {(missing > 0 ? log.ToString() : "none")}");
        }

        static AudioClip Load(string folder, string name)
        {
            foreach (var ext in new[] { ".wav", ".ogg", ".mp3" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(folder + "/" + name + ext);
                if (clip != null) return clip;
            }
            return null;
        }

        static AudioClip[] LoadAll(string folder, string prefix)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return System.Array.Empty<AudioClip>();
            return AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(p), "^" + System.Text.RegularExpressions.Regex.Escape(prefix) + "[0-9]{2}$"))
                .OrderBy(p => p, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .Where(c => c != null)
                .ToArray();
        }

        static void Tune(AudioClip clip, bool music)
        {
            if (clip == null) return;
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
            if (importer == null) return;
            var s = importer.defaultSampleSettings;
            var want = new AudioImporterSampleSettings
            {
                loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = music ? 0.45f : 0.7f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = !music
            };
            bool same = s.loadType == want.loadType && s.compressionFormat == want.compressionFormat && Mathf.Approximately(s.quality, want.quality) && s.sampleRateSetting == want.sampleRateSetting && importer.forceToMono && importer.loadInBackground == music;
            if (same) return;
            importer.defaultSampleSettings = want;
            importer.forceToMono = true;
            importer.loadInBackground = music;
            importer.SaveAndReimport();
        }
    }
}
