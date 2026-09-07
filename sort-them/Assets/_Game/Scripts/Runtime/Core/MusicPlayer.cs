using UnityEngine;

namespace SortThem
{
    public class MusicPlayer : MonoBehaviour
    {
        AudioSource _source;
        AudioClip[] _clips = System.Array.Empty<AudioClip>();

        public int TrackCount => _clips.Length;
        public int Current { get; private set; }

        public void Play(AudioClip[] clips, int index)
        {
            _clips = clips ?? System.Array.Empty<AudioClip>();
            if (_clips.Length == 0) return;
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.loop = true;
                _source.spatialBlend = 0f;
                _source.playOnAwake = false;
            }
            Current = Mathf.Clamp(index, 0, _clips.Length - 1);
            _source.clip = _clips[Current];
            ApplyVolume();
            if (_source.clip != null) _source.Play();
        }

        public void Next()
        {
            if (_clips.Length == 0) return;
            Play(_clips, (Current + 1) % _clips.Length);
            Settings.SetMusicTrack(Current);
        }

        void OnEnable() => Settings.Changed += ApplyVolume;
        void OnDisable() => Settings.Changed -= ApplyVolume;

        void ApplyVolume()
        {
            if (_source != null) _source.volume = Settings.MusicVolume;
        }
    }
}
