using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SortThem
{
    public class MusicPlayer : MonoBehaviour
    {
        static readonly AssetReferenceT<AudioClip>[] None = System.Array.Empty<AssetReferenceT<AudioClip>>();

        AudioSource _source;
        AssetReferenceT<AudioClip>[] _tracks = None;
        AsyncOperationHandle<AudioClip> _handle;
        int _request;

        public int TrackCount => _tracks.Length;
        public int Current { get; private set; }

        public void Play(AssetReferenceT<AudioClip>[] tracks, int index)
        {
            _tracks = tracks ?? None;
            if (_tracks.Length == 0) return;
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.loop = true;
                _source.spatialBlend = 0f;
                _source.playOnAwake = false;
            }
            Current = Mathf.Clamp(index, 0, _tracks.Length - 1);
            ApplyVolume();
            var track = _tracks[Current];
            if (track == null || !track.RuntimeKeyIsValid()) return;
            int request = ++_request;
            var previous = _handle;
            var handle = Addressables.LoadAssetAsync<AudioClip>(track);
            _handle = handle;
            handle.Completed += h =>
            {
                if (request != _request) return;
                if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null)
                {
                    Debug.LogWarning("SortThem: music track failed to load: " + track.RuntimeKey);
                    _handle = previous;
                    Addressables.Release(h);
                    return;
                }
                _source.clip = h.Result;
                _source.Play();
                if (previous.IsValid()) Addressables.Release(previous);
            };
        }

        public void Next()
        {
            if (_tracks.Length == 0) return;
            Play(_tracks, (Current + 1) % _tracks.Length);
            Settings.SetMusicTrack(Current);
        }

        void OnEnable() => Settings.Changed += ApplyVolume;
        void OnDisable() => Settings.Changed -= ApplyVolume;

        void OnDestroy()
        {
            _request++;
            if (_handle.IsValid()) Addressables.Release(_handle);
        }

        void ApplyVolume()
        {
            if (_source != null) _source.volume = Settings.MusicVolume;
        }
    }
}
