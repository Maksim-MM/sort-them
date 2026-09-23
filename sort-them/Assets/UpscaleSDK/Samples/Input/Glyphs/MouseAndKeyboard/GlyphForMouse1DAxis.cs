using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.ActionsSet.Default;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

public class GlyphForMouse1DAxis : MonoBehaviour
{
    [SerializeField] private Mouse1DAxis _axis;

    private TMP_Text _text;
    private Image _image;
    private InputAction<float> _action;

    private void Awake()
    {
        _text = GetComponentInChildren<TMP_Text>();
        _image = transform.GetChild(0).GetComponent<Image>();
        _text.text = $"{_axis}\n0";
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
        _action = ResolveAction(BaseMouseInputActionSet.Instance, _axis);
        if (_action == null) return;

        _image.sprite = _action.TryGetGlyph();
    }

    private void Update()
    {
        if (_action == null) return;
        _text.text = $"{_axis}\n{_action.Read()}";
    }

    private static InputAction<float> ResolveAction(BaseMouseInputActionSet set, Mouse1DAxis axis) => axis switch
    {
        Mouse1DAxis.Scroll => set.Scroll,
        Mouse1DAxis.ScrollUp => set.ScrollUp,
        Mouse1DAxis.ScrollDown => set.ScrollDown,
        Mouse1DAxis.DeltaX => set.MouseDeltaX,
        Mouse1DAxis.DeltaY => set.MouseDeltaY,
        Mouse1DAxis.PositionX => set.MousePositionX,
        Mouse1DAxis.PositionY => set.MousePositionY,
        _ => null
    };
}
