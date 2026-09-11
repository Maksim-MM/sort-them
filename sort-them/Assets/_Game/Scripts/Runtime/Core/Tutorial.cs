using System;
using UnityEngine;

namespace SortThem
{
    public enum TutorialStep { Walk, Look, Take, Place, Done }

    public class Tutorial : MonoBehaviour
    {
        public static Tutorial I;

        public float WalkDistance = 2f;
        public float LookDegrees = 90f;
        public bool ForceRun;
        public MeshGhost Outline;

        public bool Active { get; private set; }
        public TutorialStep Step { get; private set; } = TutorialStep.Done;
        public CarInstance Target { get; private set; }
        public event Action Changed;

        public static bool Running => I != null && I.Active;
        public static bool BlocksInteract => Running && I.Step < TutorialStep.Take;
        public static bool BlocksPlace => Running && I.Step < TutorialStep.Place;

        PlayerController _player;
        Inventory _inventory;
        RackController _rack;
        Vector3 _lastPos;
        Quaternion _lastRot;
        float _walked, _turned;
        int _placedAtStart;
        bool _started;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready || gm.Player == null) return;
            if (!_started)
            {
                _started = true;
                if (ForceRun || !gm.TutorialDone) Begin(gm);
            }
            if (!Active) return;
            if (gm.UiBlocking)
            {
                _lastPos = _player.transform.position;
                _lastRot = _player.CameraPivot.rotation;
                return;
            }

            switch (Step)
            {
                case TutorialStep.Walk:
                {
                    var pos = _player.transform.position;
                    var d = pos - _lastPos;
                    d.y = 0f;
                    _walked += d.magnitude;
                    _lastPos = pos;
                    if (_walked >= WalkDistance) Advance(gm);
                    break;
                }
                case TutorialStep.Look:
                {
                    var rot = _player.CameraPivot.rotation;
                    _turned += Quaternion.Angle(rot, _lastRot);
                    _lastRot = rot;
                    if (_turned >= LookDegrees) Advance(gm);
                    break;
                }
                case TutorialStep.Take:
                    if (_inventory.Items.Count > 0) { Advance(gm); break; }
                    if (Target == null || Target.State != CarState.Loose) PickTarget(gm);
                    if (Outline != null)
                    {
                        if (Target != null && Target.Filter != null)
                            Outline.Show(Target.Filter.sharedMesh, Target.transform.position, Target.transform.rotation, Target.transform.lossyScale);
                        else Outline.Hide();
                    }
                    break;
                case TutorialStep.Place:
                    if (CountPlaced(gm) > _placedAtStart) { Finish(gm); break; }
                    if (_inventory.Items.Count == 0) { SetRack(null); Step = TutorialStep.Take; Target = null; Changed?.Invoke(); break; }
                    var held = _inventory.Active;
                    if (held != null && (_rack == null || _rack.Category != held.Data.Category))
                    {
                        RackController found = null;
                        foreach (var rack in gm.Racks) if (rack.Category == held.Data.Category) { found = rack; break; }
                        SetRack(found);
                    }
                    break;
            }
        }

        void Begin(GameManager gm)
        {
            _player = gm.Player;
            _inventory = _player.GetComponent<Inventory>();
            if (_inventory == null) return;
            _lastPos = _player.transform.position;
            _lastRot = _player.CameraPivot.rotation;
            Active = true;
            Step = TutorialStep.Walk;
            Changed?.Invoke();
        }

        void Advance(GameManager gm)
        {
            Step++;
            if (Step == TutorialStep.Look) _lastRot = _player.CameraPivot.rotation;
            if (Step == TutorialStep.Take) PickTarget(gm);
            if (Step == TutorialStep.Place)
            {
                if (Outline != null) Outline.Hide();
                Target = null;
                _placedAtStart = CountPlaced(gm);
            }
            Changed?.Invoke();
        }

        void Finish(GameManager gm)
        {
            SetRack(null);
            if (Outline != null) Outline.Hide();
            Target = null;
            Active = false;
            Step = TutorialStep.Done;
            gm.TutorialDone = true;
            if (gm.Save != null) gm.Save.SaveNow("tutorial");
            Changed?.Invoke();
        }

        void SetRack(RackController rack)
        {
            if (_rack == rack) return;
            if (_rack != null) _rack.SetHighlight(false);
            _rack = rack;
            if (_rack != null) _rack.SetHighlight(true);
        }

        void PickTarget(GameManager gm)
        {
            Target = null;
            float best = float.MaxValue;
            var from = _player.transform.position;
            foreach (var car in gm.Cars)
            {
                if (car.State != CarState.Loose || car.Filter == null) continue;
                var d = car.transform.position - from;
                d.y = 0f;
                float dist = d.sqrMagnitude;
                if (dist >= best) continue;
                if (!Physics.Raycast(car.transform.position + Vector3.up * 2f, Vector3.down, out var hit, 2.5f) || hit.collider != car.Col) continue;
                best = dist;
                Target = car;
            }
        }

        static int CountPlaced(GameManager gm)
        {
            int n = 0;
            foreach (var car in gm.Cars) if (car.State == CarState.Placed) n++;
            return n;
        }
    }
}
