using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Input.ActionsSet;
using UpscaleSDK.Core.Input.ActionsSet.Builders;
using UpscaleSDK.Core.Input.Binding.Builders;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using UpscaleSDK.Core.Input.Sources.Gamepad;
using UpscaleSDK.Core.Input.Sources.Keyboard;
using UpscaleSDK.Core.Input.Sources.Mouse;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace SortThem
{
    public enum HintDevice { Keyboard, Gamepad }

    public sealed class GameAction
    {
        struct GlyphEntry { public IGlyphSource Source; public string Fallback; }

        readonly List<GlyphEntry> _glyphs = new List<GlyphEntry>();
        InputAction<bool> _press, _hold;
        InputAction<float> _axis;
        InputAction<Vector2> _vector;
        float _axisPrev;
        bool _axisPressed;
        int _axisFrame = -1;

        public string Id { get; }

        public GameAction(string id) { Id = id; }

        public GameAction Press(InputSetBuilder b, params (IInputSource<bool> Source, string Fallback)[] sources)
        {
            _press = Build(b.BindAsBool(Id + ".press"), sources);
            return this;
        }

        public GameAction Hold(InputSetBuilder b, params (IInputSource<bool> Source, string Fallback)[] sources)
        {
            _hold = Build(b.BindAsBool(Id + ".hold"), sources);
            return this;
        }

        public GameAction Axis(InputSetBuilder b, params (IInputSource<float> Source, string Fallback)[] sources)
        {
            _axis = Build(b.BindAsFloat(Id + ".axis"), sources);
            GameInput.Register(this);
            return this;
        }

        public GameAction Vector(InputSetBuilder b, float deadzone, params (IInputSource<Vector2> Source, string Fallback)[] sources)
        {
            var bb = b.BindAsVector2(Id + ".vector");
            if (deadzone > 0f) bb.WithDeadzone(deadzone);
            _vector = Build(bb, sources);
            return this;
        }

        public GameAction Glyph(IGlyphSource source, string fallback)
        {
            _glyphs.Add(new GlyphEntry { Source = source, Fallback = fallback });
            return this;
        }

        InputAction<T> Build<T>(BindingBuilder<T> bb, (IInputSource<T> Source, string Fallback)[] sources)
        {
            foreach (var s in sources)
            {
                bb.WithSource(s.Source);
                if (s.Source is IGlyphSource g) _glyphs.Add(new GlyphEntry { Source = g, Fallback = s.Fallback });
            }
            return bb.Complete();
        }

        public bool Pressed() => (_press != null && _press.Read()) || AxisPressed();
        public bool Held() => (_hold != null && _hold.Read()) || (_axis != null && _axis.Read() > 0.5f);
        public float AxisValue => _axis != null ? _axis.Read() : 0f;
        public Vector2 VectorValue => _vector != null ? _vector.Read() : Vector2.zero;

        bool AxisPressed()
        {
            if (_axis == null) return false;
            TickAxis();
            return _axisPressed;
        }

        internal void TickAxis()
        {
            if (_axis == null || _axisFrame == Time.frameCount) return;
            _axisFrame = Time.frameCount;
            float v = _axis.Read();
            _axisPressed = v > 0.5f && _axisPrev <= 0.5f;
            _axisPrev = v;
        }

        public Sprite Glyph(HintDevice device) => Glyph(device, 0);

        public Sprite Glyph(HintDevice device, int index)
        {
            var provider = GameInput.GlyphProvider;
            if (provider == null) return null;
            int n = 0;
            foreach (var g in _glyphs)
            {
                if (!Matches(g.Source.Device, device)) continue;
                if (n++ < index) continue;
                try { return g.Source.GetGlyph(provider); }
                catch { return null; }
            }
            return null;
        }

        public string Fallback(HintDevice device)
        {
            foreach (var g in _glyphs) if (Matches(g.Source.Device, device)) return g.Fallback;
            return "";
        }

        static bool Matches(DeviceType source, HintDevice device) =>
            device == HintDevice.Gamepad ? source == DeviceType.Gamepad : source != DeviceType.Gamepad;
    }

    public sealed class PlayerInputActionSet : InputActionsSet<PlayerInputActionSet>
    {
        public GameAction Move { get; private set; }
        public GameAction Look { get; private set; }
        public GameAction LookMouse { get; private set; }
        public GameAction Interact { get; private set; }
        public GameAction Place { get; private set; }
        public GameAction Jump { get; private set; }
        public GameAction Sprint { get; private set; }
        public GameAction Crouch { get; private set; }
        public GameAction NextItem { get; private set; }
        public GameAction PrevItem { get; private set; }
        public GameAction Scroll { get; private set; }
        public GameAction Ability1 { get; private set; }
        public GameAction Ability2 { get; private set; }
        public GameAction Ability3 { get; private set; }
        public GameAction[] Abilities { get; private set; }

        protected override void Initialize(InputSetBuilder b)
        {
            Move = new GameAction("Move").Vector(b, 0.15f,
                (new Gamepad2DAxisSource(Gamepad2DAxis.StickLeft), "LS"),
                (new Keyboard2DAxisSource(Keyboard2DAxis.WASD), "WASD"),
                (new Keyboard2DAxisSource(Keyboard2DAxis.Arrows), "↑↓←→"));
            Look = new GameAction("Look").Vector(b, 0.15f, (new Gamepad2DAxisSource(Gamepad2DAxis.StickRight), "RS"))
                .Glyph(new Mouse2DAxisSource(Mouse2DAxis.Delta), "Mouse");
            LookMouse = new GameAction("LookMouse").Vector(b, 0f, (new Mouse2DAxisSource(Mouse2DAxis.Delta), "Mouse"));
            Interact = new GameAction("Interact")
                .Press(b, (new MouseButtonSource(MouseButton.Left, ButtonTrigger.Press), "LMB"))
                .Axis(b, (new Gamepad1DAxisSource(Gamepad1DAxis.TriggerRight), "RT"));
            Place = new GameAction("Place")
                .Press(b, (new MouseButtonSource(MouseButton.Right, ButtonTrigger.Press), "RMB"))
                .Axis(b, (new Gamepad1DAxisSource(Gamepad1DAxis.TriggerLeft), "LT"));
            Jump = new GameAction("Jump").Press(b,
                (new KeyboardButtonSource(KeyboardKey.Space, ButtonTrigger.Press), "Space"),
                (new GamepadButtonSource(GamepadButton.ButtonSouth, ButtonTrigger.Press), "A"));
            Sprint = new GameAction("Sprint").Hold(b,
                (new KeyboardButtonSource(KeyboardKey.LeftShift, ButtonTrigger.Hold), "Shift"),
                (new GamepadButtonSource(GamepadButton.StickLeft, ButtonTrigger.Hold), "LS"));
            Crouch = new GameAction("Crouch").Press(b,
                (new KeyboardButtonSource(KeyboardKey.LeftCtrl, ButtonTrigger.Press), "Ctrl"),
                (new GamepadButtonSource(GamepadButton.ButtonEast, ButtonTrigger.Press), "B"));
            NextItem = new GameAction("NextItem").Press(b, (new GamepadButtonSource(GamepadButton.ShoulderRight, ButtonTrigger.Press), "RB"));
            PrevItem = new GameAction("PrevItem").Press(b, (new GamepadButtonSource(GamepadButton.ShoulderLeft, ButtonTrigger.Press), "LB"));
            Scroll = new GameAction("Scroll").Axis(b, (new Mouse1DAxisSource(Mouse1DAxis.Scroll), "Wheel"));
            Ability1 = new GameAction("Ability1").Press(b,
                (new KeyboardButtonSource(KeyboardKey.Digit1, ButtonTrigger.Press), "1"),
                (new GamepadButtonSource(GamepadButton.DPadUp, ButtonTrigger.Press), "↑"));
            Ability2 = new GameAction("Ability2").Press(b,
                (new KeyboardButtonSource(KeyboardKey.Digit2, ButtonTrigger.Press), "2"),
                (new GamepadButtonSource(GamepadButton.DPadLeft, ButtonTrigger.Press), "←"));
            Ability3 = new GameAction("Ability3").Press(b,
                (new KeyboardButtonSource(KeyboardKey.Digit3, ButtonTrigger.Press), "3"),
                (new GamepadButtonSource(GamepadButton.DPadRight, ButtonTrigger.Press), "→"));
            Abilities = new[] { Ability1, Ability2, Ability3 };
        }
    }

    public sealed class UiInputActionSet : InputActionsSet<UiInputActionSet>
    {
        public GameAction Navigate { get; private set; }
        public GameAction Submit { get; private set; }
        public GameAction Cancel { get; private set; }
        public GameAction Pause { get; private set; }

        protected override void Initialize(InputSetBuilder b)
        {
            Pause = new GameAction("Pause").Press(b,
                (new KeyboardButtonSource(KeyboardKey.Escape, ButtonTrigger.Press), "Esc"),
                (new GamepadButtonSource(GamepadButton.Start, ButtonTrigger.Press), "Start"));
            Navigate = new GameAction("Navigate").Vector(b, 0.5f,
                (new Gamepad2DAxisSource(Gamepad2DAxis.DPad), "DPad"),
                (new Gamepad2DAxisSource(Gamepad2DAxis.StickLeft), "LS"),
                (new Keyboard2DAxisSource(Keyboard2DAxis.Arrows), "↑↓←→"));
            Submit = new GameAction("Submit").Press(b,
                (new KeyboardButtonSource(KeyboardKey.Enter, ButtonTrigger.Press), "Enter"),
                (new GamepadActionSource(GamepadAction.Apply, ButtonTrigger.Press), "A"));
            Submit.Hold(b,
                (new KeyboardButtonSource(KeyboardKey.Enter, ButtonTrigger.Hold), "Enter"),
                (new GamepadActionSource(GamepadAction.Apply, ButtonTrigger.Hold), "A"));
            Cancel = new GameAction("Cancel").Press(b, (new GamepadActionSource(GamepadAction.Cancel, ButtonTrigger.Press), "B"));
        }
    }

    public static class GameInput
    {
        static readonly List<GameAction> AxisActions = new List<GameAction>();
        static bool _gameplayActive = true;

        public static PlayerInputActionSet Player => PlayerInputActionSet.Instance;
        public static UiInputActionSet Ui => UiInputActionSet.Instance;

        public static HintDevice Device => ActiveDevice.Gamepad ? HintDevice.Gamepad : HintDevice.Keyboard;

        public static IGlyphProvider GlyphProvider
        {
            get
            {
                try { return UPSInput.TryGetGlyphProvider(); }
                catch { return null; }
            }
        }

        public static bool GamepadConnected => UnityEngine.InputSystem.Gamepad.current != null;

        internal static void Register(GameAction action) { if (!AxisActions.Contains(action)) AxisActions.Add(action); }

        public static void Tick()
        {
            foreach (var a in AxisActions) a.TickAxis();
        }

        public static void SetGameplayActive(bool active)
        {
            if (_gameplayActive == active) return;
            _gameplayActive = active;
            if (active) Player.Activate(); else Player.Deactivate();
        }
    }
}
