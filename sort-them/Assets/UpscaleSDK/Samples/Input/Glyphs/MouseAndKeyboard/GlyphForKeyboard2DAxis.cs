using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.ActionsSet.Default;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

public class GlyphForKeyboard2DAxis : MonoBehaviour
{
    [SerializeField] private Keyboard2DAxis _axis;
    private float _activeScale = 0.8f;
    private float _idleScale = 1f;

    private Image _image;
    private InputAction<Vector2> _action;

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

        gameObject.name = $"GlyphFor_{_axis}";
    }
#endif

    private void Start()
    {
        _action = ResolveAction(BaseKeyboardActionSet.Instance, _axis);
        if (_action == null) return;

        _image.sprite = _action.TryGetGlyph();
        _action.Performed += OnActionPerformed;
    }

    private void OnDestroy()
    {
        if (_action != null) _action.Performed -= OnActionPerformed;
    }

    private void OnActionPerformed(Vector2 value)
    {
        ApplyScale(value.sqrMagnitude > 0f);
    }

    private void ApplyScale(bool active)
    {
        if (_image == null) return;
        float s = active ? _activeScale : _idleScale;
        _image.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    private static InputAction<Vector2> ResolveAction(BaseKeyboardActionSet set, Keyboard2DAxis axis) => axis switch
    {
        Keyboard2DAxis.WASD => set.WASD,
        Keyboard2DAxis.Arrows => set.Arrows,
        _ => null
    };
}
