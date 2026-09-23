using TMPro;
using UnityEngine;
using UpscaleSDK.Core.Saves;
using UpscaleSDK.Core.Ui.Runtime;

public class SaveTest : MonoBehaviour
{
    public enum SaveType
    {
        Int,
        Float,
        String,
        Bool
    }

    [Header("Setup")]
    [SerializeField] private string key;
    [SerializeField] private SaveType type;

    [Header("UI")]
    [SerializeField] private TMP_Text text;

    [Header("Colors")]
    [SerializeField] private Color focusedColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;

    private UIElement _element;
    private bool _isFocused;

    private void Start()
    {
        _element = GetComponent<UIElement>();

        _element.OnFocusChanged += OnFocusChanged;
        _element.OnInteracted += OnInteracted;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_element == null) return;

        _element.OnFocusChanged -= OnFocusChanged;
        _element.OnInteracted -= OnInteracted;
    }

    private void OnFocusChanged(bool oldValue, bool newValue)
    {
        _isFocused = newValue;
        text.color = _isFocused ? focusedColor : normalColor;
    }

    private void OnInteracted()
    {
        if (!UPSSaves.Prefs.IsReady)
            return;

        switch (type)
        {
            case SaveType.Int:
                int i = UPSSaves.Prefs.GetInt(key);
                UPSSaves.Prefs.SetInt(key, i + 1);
                break;

            case SaveType.Float:
                float f = UPSSaves.Prefs.GetFloat(key);
                UPSSaves.Prefs.SetFloat(key, f + 0.5f);
                break;

            case SaveType.String:
                string tag = GenerateRandomTag(3);
                UPSSaves.Prefs.SetString(key, tag);
                break;

            case SaveType.Bool:
                bool b = UPSSaves.Prefs.GetBool(key);
                UPSSaves.Prefs.SetBool(key, !b);
                break;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (!UPSSaves.Prefs.IsReady)
            return;

        string value = type switch
        {
            SaveType.Int => UPSSaves.Prefs.GetInt(key).ToString(),
            SaveType.Float => UPSSaves.Prefs.GetFloat(key).ToString("0.00"),
            SaveType.String => UPSSaves.Prefs.GetString(key),
            SaveType.Bool => UPSSaves.Prefs.GetBool(key).ToString(),
            _ => "N/A"
        };

        text.text = $"{key} : {value}";
    }
    
    private static readonly char[] _chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    private string GenerateRandomTag(int length)
    {
        char[] result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = _chars[Random.Range(0, _chars.Length)];
        }

        return new string(result);
    }

}
