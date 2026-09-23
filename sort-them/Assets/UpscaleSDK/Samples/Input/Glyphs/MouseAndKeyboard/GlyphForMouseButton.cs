using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.ActionsSet.Default;
using UpscaleSDK.Core.Input.ActionsSet.Groups;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

public class GlyphForMouseButton : MonoBehaviour
{
    [SerializeField] private MouseButton _button;
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

        gameObject.name = $"GlyphFor_{_button}";
    }
#endif

    private void Start()
    {
        var group = ResolveGroup(BaseMouseInputActionSet.Instance, _button);
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

    private static ButtonActionGroup ResolveGroup(BaseMouseInputActionSet set, MouseButton button) => button switch
    {
        MouseButton.Left => set.LeftClick,
        MouseButton.Right => set.RightClick,
        MouseButton.Middle => set.MiddleClick,
        _ => null
    };
}
