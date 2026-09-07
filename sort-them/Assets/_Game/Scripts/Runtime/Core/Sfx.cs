using UnityEngine;

namespace SortThem
{
    public static class Sfx
    {
        public static float Volume = 1f;

        const int PoolSize = 12;
        static AudioSource[] _pool;
        static int _next;

        public static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var src = Next();
            if (src == null) return;
            src.transform.position = position;
            src.spatialBlend = 1f;
            src.pitch = pitch;
            src.volume = Mathf.Clamp01(Volume * volume);
            src.PlayOneShot(clip);
        }

        public static void PlayUi(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var src = Next();
            if (src == null) return;
            src.spatialBlend = 0f;
            src.pitch = pitch;
            src.volume = Mathf.Clamp01(Volume * volume);
            src.PlayOneShot(clip);
        }

        static AudioSource Next()
        {
            if (_pool == null || _pool.Length == 0 || _pool[0] == null)
            {
                var root = new GameObject("Sfx");
                Object.DontDestroyOnLoad(root);
                _pool = new AudioSource[PoolSize];
                for (int i = 0; i < PoolSize; i++)
                {
                    var go = new GameObject("Source" + i);
                    go.transform.SetParent(root.transform, false);
                    var s = go.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.rolloffMode = AudioRolloffMode.Linear;
                    s.minDistance = 1.5f;
                    s.maxDistance = 25f;
                    s.dopplerLevel = 0f;
                    _pool[i] = s;
                }
            }
            var src = _pool[_next];
            _next = (_next + 1) % PoolSize;
            return src;
        }
    }
}
