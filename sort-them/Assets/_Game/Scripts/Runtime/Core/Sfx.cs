using UnityEngine;

namespace SortThem
{
    public static class Sfx
    {
        public static float Volume = 1f;

        public static void Play(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, Volume);
        }
    }
}
