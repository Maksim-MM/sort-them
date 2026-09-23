using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.ActionsSet.Default;
using UpscaleSDK.Core.Input.ActionsSet.Groups;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

public class GlyphForKeyboardButton : MonoBehaviour
{
    [SerializeField] private KeyboardKey _key;
    private float _activeScale = 0.8f;
    private float _idleScale = 1f;

    private Image _image;
    private InputAction<bool> _press;
    private InputAction<bool> _release;

    private void Awake()
    {
        _image = transform.GetChild(0).GetComponent<Image>();
        ApplyScale(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            return;

        gameObject.name = $"GlyphFor_{_key}";
    }
#endif

    private void Start()
    {
        var group = ResolveGroup(BaseKeyboardActionSet.Instance, _key);
        if (group == null) return;

        _press = group.Press;
        _release = group.Release;

        _press.Performed += OnPress;
        _release.Performed += OnRelease;

        _image.sprite = _press.TryGetGlyph();
    }

    private void OnDestroy()
    {
        if (_press != null) _press.Performed -= OnPress;
        if (_release != null) _release.Performed -= OnRelease;
    }

    private void OnPress(bool _) => ApplyScale(true);
    private void OnRelease(bool _) => ApplyScale(false);

    private void ApplyScale(bool active)
    {
        if (_image == null) return;
        float s = active ? _activeScale : _idleScale;
        _image.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    private static ButtonActionGroup ResolveGroup(BaseKeyboardActionSet set, KeyboardKey key) => key switch
    {
        KeyboardKey.Space => set.Space,
        KeyboardKey.Enter => set.Enter,
        KeyboardKey.Escape => set.Escape,
        KeyboardKey.Tab => set.Tab,
        KeyboardKey.Backspace => set.Backspace,
        KeyboardKey.Delete => set.Delete,
        KeyboardKey.Insert => set.Insert,
        KeyboardKey.Home => set.Home,
        KeyboardKey.End => set.End,

        KeyboardKey.LeftShift => set.LeftShift,
        KeyboardKey.RightShift => set.RightShift,
        KeyboardKey.LeftCtrl => set.LeftCtrl,
        KeyboardKey.RightCtrl => set.RightCtrl,
        KeyboardKey.LeftAlt => set.LeftAlt,
        KeyboardKey.RightAlt => set.RightAlt,

        KeyboardKey.UpArrow => set.UpArrow,
        KeyboardKey.DownArrow => set.DownArrow,
        KeyboardKey.LeftArrow => set.LeftArrow,
        KeyboardKey.RightArrow => set.RightArrow,

        KeyboardKey.A => set.KeyA,
        KeyboardKey.B => set.KeyB,
        KeyboardKey.C => set.KeyC,
        KeyboardKey.D => set.KeyD,
        KeyboardKey.E => set.KeyE,
        KeyboardKey.F => set.KeyF,
        KeyboardKey.G => set.KeyG,
        KeyboardKey.H => set.KeyH,
        KeyboardKey.I => set.KeyI,
        KeyboardKey.J => set.KeyJ,
        KeyboardKey.K => set.KeyK,
        KeyboardKey.L => set.KeyL,
        KeyboardKey.M => set.KeyM,
        KeyboardKey.N => set.KeyN,
        KeyboardKey.O => set.KeyO,
        KeyboardKey.P => set.KeyP,
        KeyboardKey.Q => set.KeyQ,
        KeyboardKey.R => set.KeyR,
        KeyboardKey.S => set.KeyS,
        KeyboardKey.T => set.KeyT,
        KeyboardKey.U => set.KeyU,
        KeyboardKey.V => set.KeyV,
        KeyboardKey.W => set.KeyW,
        KeyboardKey.X => set.KeyX,
        KeyboardKey.Y => set.KeyY,
        KeyboardKey.Z => set.KeyZ,

        KeyboardKey.Digit0 => set.Digit0,
        KeyboardKey.Digit1 => set.Digit1,
        KeyboardKey.Digit2 => set.Digit2,
        KeyboardKey.Digit3 => set.Digit3,
        KeyboardKey.Digit4 => set.Digit4,
        KeyboardKey.Digit5 => set.Digit5,
        KeyboardKey.Digit6 => set.Digit6,
        KeyboardKey.Digit7 => set.Digit7,
        KeyboardKey.Digit8 => set.Digit8,
        KeyboardKey.Digit9 => set.Digit9,

        KeyboardKey.F1 => set.F1,
        KeyboardKey.F2 => set.F2,
        KeyboardKey.F3 => set.F3,
        KeyboardKey.F4 => set.F4,
        KeyboardKey.F5 => set.F5,
        KeyboardKey.F6 => set.F6,
        KeyboardKey.F7 => set.F7,
        KeyboardKey.F8 => set.F8,
        KeyboardKey.F9 => set.F9,
        KeyboardKey.F10 => set.F10,
        KeyboardKey.F11 => set.F11,
        KeyboardKey.F12 => set.F12,

        KeyboardKey.Comma => set.Comma,
        KeyboardKey.Slash => set.Slash,
        KeyboardKey.Semicolon => set.Semicolon,
        KeyboardKey.Quote => set.Quote,
        KeyboardKey.LeftBracket => set.LeftBracket,
        KeyboardKey.RightBracket => set.RightBracket,
        KeyboardKey.Backslash => set.Backslash,
        KeyboardKey.Equals => set.EqualsKey,

        _ => null
    };
}
