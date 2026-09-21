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

        public bool Active { get; private set; }
        public TutorialStep Step { get; private set; } = TutorialStep.Done;
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
                    if (_inventory.Items.Count > 0) Advance(gm);
                    break;
                case TutorialStep.Place:
                    if (CountPlaced(gm) > _placedAtStart) { Finish(gm); break; }
                    if (_inventory.Items.Count == 0) { SetRack(null); Step = TutorialStep.Take; Changed?.Invoke(); break; }
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
            if (Step == TutorialStep.Place) _placedAtStart = CountPlaced(gm);
            Changed?.Invoke();
        }

        void Finish(GameManager gm)
        {
            SetRack(null);
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

        static int CountPlaced(GameManager gm)
        {
            int n = 0;
            foreach (var car in gm.Cars) if (car.State == CarState.Placed) n++;
            return n;
        }
    }
}
