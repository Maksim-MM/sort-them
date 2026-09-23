using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.ActionsSet.Default;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

public class GlyphForGamepad2DAxis : MonoBehaviour
{
    [SerializeField] private Gamepad2DAxis _axis;

    private TMP_Text _text;
    private Image _image;
    private InputAction<Vector2> _action;

    private void Awake()
    {
        _text = GetComponentInChildren<TMP_Text>();
        _image = transform.GetChild(0).GetComponent<Image>();
        _text.text = $"{_axis}\n{Vector2.zero}";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            return;

        gameObject.name = $"GlyphFor_{_axis}";
    }
#endif

    private void Start()
    {
        _action = ResolveAction(BaseGamepadActionSet.Instance, _axis);
        if (_action == null) return;

        _image.sprite = _action.TryGetGlyph();
    }

    private void Update()
    {
        if (_action == null) return;
        _text.text = $"{_axis}\n{_action.Read()}";
    }

    private static InputAction<Vector2> ResolveAction(BaseGamepadActionSet set, Gamepad2DAxis axis) => axis switch
    {
        Gamepad2DAxis.DPad => set.DPad,
        Gamepad2DAxis.StickLeft => set.LeftStick,
        Gamepad2DAxis.StickRight => set.RightStick,
        _ => null
    };
}
