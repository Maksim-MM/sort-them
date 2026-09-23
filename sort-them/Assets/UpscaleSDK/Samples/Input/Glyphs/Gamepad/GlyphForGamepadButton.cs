using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.ActionsSet.Default;
using UpscaleSDK.Core.Input.ActionsSet.Groups;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

public class GlyphForGamepadButton : MonoBehaviour
{
    [SerializeField] private GamepadButton _button;
    private float _activeScale = 0.8f;
    private float _idleScale = 1f;

    private TMP_Text _text;
    private Image _image;
    private InputAction<bool> _press;
    private InputAction<bool> _release;

    private void Awake()
    {
        _text = GetComponentInChildren<TMP_Text>();
        _image = transform.GetChild(0).GetComponent<Image>();
        _text.text = _button.ToString();
        ApplyScale(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            return;

        gameObject.name = $"GlyphFor_{_button}";
    }
#endif

    private void Start()
    {
        var group = ResolveGroup(BaseGamepadActionSet.Instance, _button);
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

    private static ButtonActionGroup ResolveGroup(BaseGamepadActionSet set, GamepadButton button) => button switch
    {
        GamepadButton.ButtonNorth => set.North,
        GamepadButton.ButtonSouth => set.South,
        GamepadButton.ButtonEast => set.East,
        GamepadButton.ButtonWest => set.West,
        GamepadButton.StickLeft => set.LeftStickButton,
        GamepadButton.StickRight => set.RightStickButton,
        GamepadButton.ShoulderLeft => set.LeftShoulder,
        GamepadButton.ShoulderRight => set.RightShoulder,
        GamepadButton.Start => set.Start,
        GamepadButton.Select => set.Select,
        GamepadButton.DPadUp => set.DpadUp,
        GamepadButton.DPadDown => set.DpadDown,
        GamepadButton.DPadLeft => set.DpadLeft,
        GamepadButton.DPadRight => set.DpadRight,
        _ => null
    };
}
