using System;
using System.Collections;
using UnityEngine;

namespace SortThem
{
    public class BlueprintWall : MonoBehaviour
    {
        public CategoryData[] Categories = Array.Empty<CategoryData>();
        public GameObject[] Frames = Array.Empty<GameObject>();

        GameManager _gm;
        bool[] _shown;

        void Start()
        {
            _shown = new bool[Frames.Length];
            for (int i = 0; i < Frames.Length; i++) if (Frames[i] != null) Frames[i].SetActive(false);
            _gm = GameManager.I;
            if (_gm == null) return;
            _gm.StatsChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.StatsChanged -= Refresh;
        }

        void Refresh()
        {
            if (_gm == null || _gm.Catalog == null) return;
            int count = Mathf.Min(Frames.Length, Categories.Length);
            for (int i = 0; i < count; i++)
            {
                if (_shown[i] || Frames[i] == null || Categories[i] == null) continue;
                int bit = Array.IndexOf(_gm.Catalog.Categories, Categories[i]);
                if (bit < 0) continue;
                if (_gm.HasBlueprint(bit)) { Show(i, false); continue; }
                if (!_gm.Ready || !IsCategoryClosed(Categories[i])) continue;
                _gm.UnlockBlueprint(bit);
                Show(i, true);
            }
        }

        bool IsCategoryClosed(CategoryData category)
        {
            bool any = false;
            foreach (var rack in _gm.Racks)
            {
                if (rack == null || rack.Category != category || rack.Shelves == null) continue;
                foreach (var shelf in rack.Shelves)
                {
                    if (shelf == null) continue;
                    any = true;
                    if (!shelf.IsClosed) return false;
                }
            }
            return any;
        }

        void Show(int index, bool animate)
        {
            _shown[index] = true;
            var frame = Frames[index];
            frame.SetActive(true);
            frame.transform.localScale = Vector3.one;
            if (animate && isActiveAndEnabled && gameObject.activeInHierarchy) StartCoroutine(Appear(frame.transform));
        }

        IEnumerator Appear(Transform target)
        {
            const float duration = 0.45f;
            float time = 0f;
            while (time < duration && target != null)
            {
                time += Time.deltaTime;
                float p = Mathf.Clamp01(time / duration);
                float scale = Mathf.Lerp(0.55f, 1f, 1f - Mathf.Pow(1f - p, 3f)) + Mathf.Sin(p * Mathf.PI) * 0.1f;
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }
    }
}
