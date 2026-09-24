using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public class GlyphImage : MonoBehaviour
    {
        public GameAction Action;
        public int Index;
        public Image Image;
        public TMP_Text Fallback;
        public bool HideWhenEmpty = true;

        HintDevice _device;
        Sprite _sprite;
        bool _init;

        public void Set(GameAction action, int index = 0)
        {
            Action = action;
            Index = index;
            _init = false;
            Refresh();
        }

        void OnEnable() { _init = false; Refresh(); }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            var device = ControlHints.Device;
            var sprite = ControlHints.Glyph(Action, Index);
            if (_init && device == _device && sprite == _sprite) return;
            _init = true;
            _device = device;
            _sprite = sprite;
            bool has = sprite != null;
            if (Image != null)
            {
                Image.sprite = sprite;
                Image.enabled = has;
                var size = GetComponent<LayoutElement>();
                if (has && size != null && size.preferredHeight > 0f)
                {
                    float aspect = Mathf.Clamp(sprite.rect.width / Mathf.Max(1f, sprite.rect.height), 1f, 3.5f);
                    float dense = device == HintDevice.Gamepad || (sprite.texture != null && sprite.texture.width <= 128) ? 0.7f : 1f;
                    float h = size.preferredHeight * dense;
                    size.preferredWidth = size.minWidth = h * aspect;
                    Image.rectTransform.sizeDelta = new Vector2(size.preferredWidth, h);
                }
            }
            string text = has || Action == null ? "" : Index == 0 ? Action.Fallback(device) : "";
            if (Fallback != null)
            {
                Fallback.text = text;
                Fallback.gameObject.SetActive(!has && text.Length > 0);
            }
            bool visible = has || text.Length > 0 || !HideWhenEmpty;
            var le = GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = !visible;
            if (Image != null && !visible && Fallback == null) Image.enabled = false;
        }
    }
}
