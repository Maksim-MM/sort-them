using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem
{
    public class Bomb : MonoBehaviour
    {
        public Transform Fuse;
        public Transform Tip;
        public AudioSource Hiss;

        public bool Lit { get; private set; }
        public bool Held { get; private set; }
        public float FuseLeft { get; private set; }

        float _fuseTotal;
        Vector3 _fuseScale, _fusePos, _tipPos;
        Rigidbody _body;
        Collider _col;
        ParticleSystem _sparks;

        public Rigidbody Body => _body != null ? _body : _body = GetComponent<Rigidbody>();

        void Awake()
        {
            _col = GetComponent<Collider>();
            if (Fuse != null) { _fuseScale = Fuse.localScale; _fusePos = Fuse.localPosition; }
            if (Tip != null) { _tipPos = Tip.localPosition; Tip.gameObject.SetActive(false); }
        }

        public void Ignite(float seconds, AudioClip hiss)
        {
            if (Lit) return;
            Lit = true;
            _fuseTotal = Mathf.Max(0.1f, seconds);
            FuseLeft = _fuseTotal;
            if (Tip != null) Tip.gameObject.SetActive(true);
            StartSparks();
            if (Hiss != null && hiss != null)
            {
                Hiss.clip = hiss;
                Hiss.loop = true;
                Hiss.volume = Sfx.Volume;
                Hiss.Play();
            }
        }

        void StartSparks()
        {
            if (Tip == null || _sparks != null) return;
            var go = new GameObject("Sparks");
            go.transform.SetParent(Tip, false);
            _sparks = go.AddComponent<ParticleSystem>();
            _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _sparks.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), new Color(1f, 0.5f, 0.1f));
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 60;
            var emission = _sparks.emission;
            emission.rateOverTime = 45f;
            var shape = _sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.01f;
            var col = _sparks.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 2.5f;
            var tipRenderer = Tip.GetComponent<Renderer>();
            if (tipRenderer != null) r.sharedMaterial = tipRenderer.sharedMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            _sparks.Play();
        }

        public void Hold(Transform hand, Vector3 localPosition, Quaternion localRotation)
        {
            Held = true;
            Body.isKinematic = true;
            if (_col != null) _col.enabled = false;
            transform.SetParent(hand, false);
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
        }

        public void Release(Transform root, Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            Held = false;
            transform.SetParent(root, true);
            transform.SetPositionAndRotation(position, rotation);
            if (_col != null) _col.enabled = true;
            Body.isKinematic = false;
            Body.WakeUp();
            Body.linearVelocity = velocity;
            Body.angularVelocity = Random.insideUnitSphere * 4f;
        }

        void Update()
        {
            if (!Lit) return;
            FuseLeft -= Time.deltaTime;
            float k = Mathf.Clamp01(FuseLeft / _fuseTotal);
            if (Fuse != null)
            {
                Fuse.localScale = new Vector3(_fuseScale.x, _fuseScale.y * k, _fuseScale.z);
                Fuse.localPosition = _fusePos * k;
            }
            if (Tip != null)
            {
                Tip.localPosition = _tipPos * k;
                float flicker = 0.8f + 0.4f * Mathf.PerlinNoise(Time.time * 18f, 0.3f);
                Tip.localScale = Vector3.one * 0.25f * flicker;
            }
            if (FuseLeft <= 0f)
            {
                var gm = GameManager.I;
                if (gm != null) gm.ExplodeBomb(this); else Destroy(gameObject);
            }
        }
    }
}
