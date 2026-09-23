using UnityEngine;
using UpscaleSDK.Core.Input.ActionsSet.Builders;
using UpscaleSDK.Core.Input.ActionsSet.Extensions;
using UpscaleSDK.Core.Input.ActionsSet.Groups;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

namespace UpscaleSDK.Core.Input.ActionsSet.Default
{
    public class BaseKeyboardActionSet : InputActionsSet<BaseKeyboardActionSet>
    {
        /// <summary>WASD composite (X = D - A, Y = W - S).</summary>
        public InputAction<Vector2> WASD { get; private set; }
        /// <summary>Arrow keys composite (X = Right - Left, Y = Up - Down).</summary>
        public InputAction<Vector2> Arrows { get; private set; }

        // Common named keys
        public ButtonActionGroup Space { get; private set; }
        public ButtonActionGroup Enter { get; private set; }
        public ButtonActionGroup Escape { get; private set; }
        public ButtonActionGroup Tab { get; private set; }
        public ButtonActionGroup Backspace { get; private set; }
        public ButtonActionGroup Delete { get; private set; }
        public ButtonActionGroup Insert { get; private set; }
        public ButtonActionGroup Home { get; private set; }
        public ButtonActionGroup End { get; private set; }

        // Modifiers
        public ButtonActionGroup LeftShift { get; private set; }
        public ButtonActionGroup RightShift { get; private set; }
        public ButtonActionGroup LeftCtrl { get; private set; }
        public ButtonActionGroup RightCtrl { get; private set; }
        public ButtonActionGroup LeftAlt { get; private set; }
        public ButtonActionGroup RightAlt { get; private set; }

        // Arrow keys (individual)
        public ButtonActionGroup UpArrow { get; private set; }
        public ButtonActionGroup DownArrow { get; private set; }
        public ButtonActionGroup LeftArrow { get; private set; }
        public ButtonActionGroup RightArrow { get; private set; }

        // Letters
        public ButtonActionGroup KeyA { get; private set; }
        public ButtonActionGroup KeyB { get; private set; }
        public ButtonActionGroup KeyC { get; private set; }
        public ButtonActionGroup KeyD { get; private set; }
        public ButtonActionGroup KeyE { get; private set; }
        public ButtonActionGroup KeyF { get; private set; }
        public ButtonActionGroup KeyG { get; private set; }
        public ButtonActionGroup KeyH { get; private set; }
        public ButtonActionGroup KeyI { get; private set; }
        public ButtonActionGroup KeyJ { get; private set; }
        public ButtonActionGroup KeyK { get; private set; }
        public ButtonActionGroup KeyL { get; private set; }
        public ButtonActionGroup KeyM { get; private set; }
        public ButtonActionGroup KeyN { get; private set; }
        public ButtonActionGroup KeyO { get; private set; }
        public ButtonActionGroup KeyP { get; private set; }
        public ButtonActionGroup KeyQ { get; private set; }
        public ButtonActionGroup KeyR { get; private set; }
        public ButtonActionGroup KeyS { get; private set; }
        public ButtonActionGroup KeyT { get; private set; }
        public ButtonActionGroup KeyU { get; private set; }
        public ButtonActionGroup KeyV { get; private set; }
        public ButtonActionGroup KeyW { get; private set; }
        public ButtonActionGroup KeyX { get; private set; }
        public ButtonActionGroup KeyY { get; private set; }
        public ButtonActionGroup KeyZ { get; private set; }

        // Digits
        public ButtonActionGroup Digit0 { get; private set; }
        public ButtonActionGroup Digit1 { get; private set; }
        public ButtonActionGroup Digit2 { get; private set; }
        public ButtonActionGroup Digit3 { get; private set; }
        public ButtonActionGroup Digit4 { get; private set; }
        public ButtonActionGroup Digit5 { get; private set; }
        public ButtonActionGroup Digit6 { get; private set; }
        public ButtonActionGroup Digit7 { get; private set; }
        public ButtonActionGroup Digit8 { get; private set; }
        public ButtonActionGroup Digit9 { get; private set; }

        // Function keys
        public ButtonActionGroup F1 { get; private set; }
        public ButtonActionGroup F2 { get; private set; }
        public ButtonActionGroup F3 { get; private set; }
        public ButtonActionGroup F4 { get; private set; }
        public ButtonActionGroup F5 { get; private set; }
        public ButtonActionGroup F6 { get; private set; }
        public ButtonActionGroup F7 { get; private set; }
        public ButtonActionGroup F8 { get; private set; }
        public ButtonActionGroup F9 { get; private set; }
        public ButtonActionGroup F10 { get; private set; }
        public ButtonActionGroup F11 { get; private set; }
        public ButtonActionGroup F12 { get; private set; }

        // Symbols
        public ButtonActionGroup Comma { get; private set; }
        public ButtonActionGroup Slash { get; private set; }
        public ButtonActionGroup Semicolon { get; private set; }
        public ButtonActionGroup Quote { get; private set; }
        public ButtonActionGroup LeftBracket { get; private set; }
        public ButtonActionGroup RightBracket { get; private set; }
        public ButtonActionGroup Backslash { get; private set; }
        public ButtonActionGroup EqualsKey { get; private set; }

        protected override void Initialize(InputSetBuilder builder)
        {
            WASD = builder.BindAsVector2().ToWASD().Complete();
            Arrows = builder.BindAsVector2().ToArrows().Complete();

            Space = builder.BindKeyGroup(KeyboardKey.Space);
            Enter = builder.BindKeyGroup(KeyboardKey.Enter);
            Escape = builder.BindKeyGroup(KeyboardKey.Escape);
            Tab = builder.BindKeyGroup(KeyboardKey.Tab);
            Backspace = builder.BindKeyGroup(KeyboardKey.Backspace);
            Delete = builder.BindKeyGroup(KeyboardKey.Delete);
            Insert = builder.BindKeyGroup(KeyboardKey.Insert);
            Home = builder.BindKeyGroup(KeyboardKey.Home);
            End = builder.BindKeyGroup(KeyboardKey.End);

            LeftShift = builder.BindKeyGroup(KeyboardKey.LeftShift);
            RightShift = builder.BindKeyGroup(KeyboardKey.RightShift);
            LeftCtrl = builder.BindKeyGroup(KeyboardKey.LeftCtrl);
            RightCtrl = builder.BindKeyGroup(KeyboardKey.RightCtrl);
            LeftAlt = builder.BindKeyGroup(KeyboardKey.LeftAlt);
            RightAlt = builder.BindKeyGroup(KeyboardKey.RightAlt);

            UpArrow = builder.BindKeyGroup(KeyboardKey.UpArrow);
            DownArrow = builder.BindKeyGroup(KeyboardKey.DownArrow);
            LeftArrow = builder.BindKeyGroup(KeyboardKey.LeftArrow);
            RightArrow = builder.BindKeyGroup(KeyboardKey.RightArrow);

            KeyA = builder.BindKeyGroup(KeyboardKey.A);
            KeyB = builder.BindKeyGroup(KeyboardKey.B);
            KeyC = builder.BindKeyGroup(KeyboardKey.C);
            KeyD = builder.BindKeyGroup(KeyboardKey.D);
            KeyE = builder.BindKeyGroup(KeyboardKey.E);
            KeyF = builder.BindKeyGroup(KeyboardKey.F);
            KeyG = builder.BindKeyGroup(KeyboardKey.G);
            KeyH = builder.BindKeyGroup(KeyboardKey.H);
            KeyI = builder.BindKeyGroup(KeyboardKey.I);
            KeyJ = builder.BindKeyGroup(KeyboardKey.J);
            KeyK = builder.BindKeyGroup(KeyboardKey.K);
            KeyL = builder.BindKeyGroup(KeyboardKey.L);
            KeyM = builder.BindKeyGroup(KeyboardKey.M);
            KeyN = builder.BindKeyGroup(KeyboardKey.N);
            KeyO = builder.BindKeyGroup(KeyboardKey.O);
            KeyP = builder.BindKeyGroup(KeyboardKey.P);
            KeyQ = builder.BindKeyGroup(KeyboardKey.Q);
            KeyR = builder.BindKeyGroup(KeyboardKey.R);
            KeyS = builder.BindKeyGroup(KeyboardKey.S);
            KeyT = builder.BindKeyGroup(KeyboardKey.T);
            KeyU = builder.BindKeyGroup(KeyboardKey.U);
            KeyV = builder.BindKeyGroup(KeyboardKey.V);
            KeyW = builder.BindKeyGroup(KeyboardKey.W);
            KeyX = builder.BindKeyGroup(KeyboardKey.X);
            KeyY = builder.BindKeyGroup(KeyboardKey.Y);
            KeyZ = builder.BindKeyGroup(KeyboardKey.Z);

            Digit0 = builder.BindKeyGroup(KeyboardKey.Digit0);
            Digit1 = builder.BindKeyGroup(KeyboardKey.Digit1);
            Digit2 = builder.BindKeyGroup(KeyboardKey.Digit2);
            Digit3 = builder.BindKeyGroup(KeyboardKey.Digit3);
            Digit4 = builder.BindKeyGroup(KeyboardKey.Digit4);
            Digit5 = builder.BindKeyGroup(KeyboardKey.Digit5);
            Digit6 = builder.BindKeyGroup(KeyboardKey.Digit6);
            Digit7 = builder.BindKeyGroup(KeyboardKey.Digit7);
            Digit8 = builder.BindKeyGroup(KeyboardKey.Digit8);
            Digit9 = builder.BindKeyGroup(KeyboardKey.Digit9);

            F1 = builder.BindKeyGroup(KeyboardKey.F1);
            F2 = builder.BindKeyGroup(KeyboardKey.F2);
            F3 = builder.BindKeyGroup(KeyboardKey.F3);
            F4 = builder.BindKeyGroup(KeyboardKey.F4);
            F5 = builder.BindKeyGroup(KeyboardKey.F5);
            F6 = builder.BindKeyGroup(KeyboardKey.F6);
            F7 = builder.BindKeyGroup(KeyboardKey.F7);
            F8 = builder.BindKeyGroup(KeyboardKey.F8);
            F9 = builder.BindKeyGroup(KeyboardKey.F9);
            F10 = builder.BindKeyGroup(KeyboardKey.F10);
            F11 = builder.BindKeyGroup(KeyboardKey.F11);
            F12 = builder.BindKeyGroup(KeyboardKey.F12);

            Comma = builder.BindKeyGroup(KeyboardKey.Comma);
            Slash = builder.BindKeyGroup(KeyboardKey.Slash);
            Semicolon = builder.BindKeyGroup(KeyboardKey.Semicolon);
            Quote = builder.BindKeyGroup(KeyboardKey.Quote);
            LeftBracket = builder.BindKeyGroup(KeyboardKey.LeftBracket);
            RightBracket = builder.BindKeyGroup(KeyboardKey.RightBracket);
            Backslash = builder.BindKeyGroup(KeyboardKey.Backslash);
            EqualsKey = builder.BindKeyGroup(KeyboardKey.Equals);
        }
    }
}
