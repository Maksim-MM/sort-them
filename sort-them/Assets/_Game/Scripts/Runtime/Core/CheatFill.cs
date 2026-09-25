#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem
{
    public class CheatFill : MonoBehaviour
    {
        static CheatFill _instance;

        CarInstance _target;
        MeshGhost _ghost;

        public static void FillAllButOne()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) return;

            var inventory = gm.Inventory;
            while (inventory != null && inventory.Items.Count > 0)
            {
                var held = inventory.RemoveActive();
                if (held == null) break;
                held.SetLoose(held.transform.position, held.transform.rotation, true);
            }

            Vector3 origin = gm.Player != null ? gm.Player.transform.position : Vector3.zero;
            CarInstance keep = null;
            float best = float.MaxValue;
            foreach (var car in gm.Cars)
            {
                if (car == null || car.Data == null || car.State != CarState.Loose) continue;
                float distance = (car.transform.position - origin).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                keep = car;
            }
            if (keep == null) { Debug.Log("SortThem cheat: no loose car to keep"); return; }

            int placed = 0, failed = 0;
            foreach (var car in gm.Cars)
            {
                if (car == null || car == keep || car.Data == null || car.State == CarState.Placed) continue;
                var rack = RackFor(gm, car.Data.Category);
                var shelf = rack != null ? rack.FindAutoPlaceShelf(car.Data) : null;
                int slot = shelf != null ? shelf.FirstFreeSlot() : -1;
                if (slot >= 0 && shelf.TryPlace(car, slot, false)) placed++;
                else failed++;
            }

            gm.RecountStats();
            Highlight(keep);
            if (gm.Save != null) gm.Save.SaveNow("cheat fill");
            Debug.Log("SortThem cheat: placed " + placed + ", failed " + failed + ", shelves " + gm.ClosedShelves + "/" + gm.TotalShelves +
                      ", left " + keep.Data.CarID + " (" + (keep.Data.Category != null ? keep.Data.Category.CategoryID : "?") + ")");
        }

        static RackController RackFor(GameManager gm, CategoryData category)
        {
            if (category == null) return null;
            foreach (var rack in gm.Racks) if (rack != null && rack.Category == category) return rack;
            return null;
        }

        static void Highlight(CarInstance car)
        {
            if (_instance == null)
            {
                var root = new GameObject("CheatHighlight");
                _instance = root.AddComponent<CheatFill>();
                _instance.BuildGhost();
            }
            _instance._target = car;
        }

        void BuildGhost()
        {
            var go = new GameObject("Outline");
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var shader = Shader.Find("SortThem/OutlineXRay");
            if (shader != null)
            {
                var material = new Material(shader) { name = "CheatOutline" };
                if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(1f, 0.85f, 0.2f));
                renderer.sharedMaterial = material;
            }
            _ghost = go.AddComponent<MeshGhost>();
            _ghost.Filter = filter;
            _ghost.Renderer = renderer;
            go.SetActive(false);
        }

        void LateUpdate()
        {
            if (_ghost == null) return;
            if (_target == null || _target.State == CarState.Placed)
            {
                _ghost.Hide();
                _target = null;
                return;
            }
            if (_target.State == CarState.Held) { _ghost.Hide(); return; }
            var filter = _target.Filter;
            if (filter == null || filter.sharedMesh == null) { _ghost.Hide(); return; }
            var t = _target.transform;
            _ghost.Show(filter.sharedMesh, t.position, t.rotation, t.lossyScale);
        }
    }
}
#endif
