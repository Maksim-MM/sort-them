using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Ui.Runtime;

namespace UpscaleSDK.Core.Ui.Samples.Base
{
    public class UIFeedbackDemo : MonoBehaviour
    {
        [Header("Focus Colors")]
        [SerializeField] private Color focusColor = Color.white;
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f);
    
        [Header("Interaction")]
        [SerializeField] private Color flashColor = new Color(1f, 1f, 0.8f);
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float animationSpeed = 8f;
        [SerializeField] private Graphic _graphic;

        private UIElement _element;
        private bool _isFocused;
        private Coroutine _currentAnimation;

        private void Start()
        {
            _element = GetComponent<UIElement>();

            if (_graphic == null) 
                _graphic = GetComponent<Graphic>() ?? GetComponentInChildren<Graphic>();
           
        
            _element.OnFocusChanged += OnFocus;
            _element.OnInteracted += OnInteracted;
        
            UpdateColorImmediate();
        }

        private void OnDestroy()
        {
            if (_element != null)
            {
                _element.OnFocusChanged -= OnFocus;
                _element.OnInteracted -= OnInteracted;
            }
        }
    
        private void OnEnable()
        {
            if (_graphic != null)
                UpdateColorImmediate();
        }
    
        private void OnFocus(bool oldValue, bool newValue)
        {
            _isFocused = newValue;
        
            if (gameObject.activeInHierarchy)
            {
                if (_currentAnimation != null)
                    StopCoroutine(_currentAnimation);
            
                _currentAnimation = StartCoroutine(AnimateToFocusState());
            }
            else
            {
                UpdateColorImmediate();
            }
        }
    
        private void OnInteracted()
        {
            if (gameObject.activeInHierarchy)
            {
                if (_currentAnimation != null)
                    StopCoroutine(_currentAnimation);
                
                _currentAnimation = StartCoroutine(FlashEffect());
            }
        }
    
        private void UpdateColorImmediate()
        {
            if (_graphic == null) return;
        
            _graphic.color = GetTargetColor();
        }
    
        private Color GetTargetColor()
        {
            return _isFocused ? focusColor : normalColor;
        }
        
        private IEnumerator AnimateToFocusState()
        {
            if (_graphic == null) yield break;
        
            Color targetColor = GetTargetColor();
        
            while (Mathf.Abs(_graphic.color.r - targetColor.r) > 0.01f ||
                   Mathf.Abs(_graphic.color.g - targetColor.g) > 0.01f ||
                   Mathf.Abs(_graphic.color.b - targetColor.b) > 0.01f)
            {
                _graphic.color = Color.Lerp(_graphic.color, targetColor, Time.deltaTime * animationSpeed);
                yield return null;
            }
        
            _graphic.color = targetColor;
            _currentAnimation = null;
        }
    
        private IEnumerator FlashEffect()
        {
            if (_graphic == null) yield break;
        
            Color startColor = _graphic.color;
            float elapsed = 0f;
        
            while (elapsed < flashDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (flashDuration / 2f);
                _graphic.color = Color.Lerp(startColor, flashColor, t);
                yield return null;
            }
        
            Color targetColor = GetTargetColor();
            elapsed = 0f;
        
            while (elapsed < flashDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (flashDuration / 2f);
                _graphic.color = Color.Lerp(flashColor, targetColor, t);
                yield return null;
            }
        
            _graphic.color = targetColor;
            _currentAnimation = null;
        }
    }
}