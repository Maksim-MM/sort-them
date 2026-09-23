using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UpscaleSDK.Core.Saves;

namespace UpscaleSDK.BootstrapScene
{
    /// <summary>
    /// Represents a ups initialize awaiter class.
    /// </summary>
    public class UPSInitializeAwaiter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup[] _logos;
        [SerializeField] private Image _background;
        private bool _isSplashCompleted;
        private float _splashDelayDuration = 3f;

        private float _splashFadeDuration = 1f;

        private void Awake()
        {
            DontDestroyOnLoad(this);

#if UNITY_EDITOR
            _splashFadeDuration = 0.25f;
            _splashDelayDuration = 0.5f;
#endif
        }

        private void Start()
        {
            StartCoroutine(CheckStartupConditions());
            StartCoroutine(PlaySplashAnimations());
        }

        private IEnumerator CheckStartupConditions()
        {
            var asyncOperation = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;

            while (!asyncOperation.isDone)
            {
                if (asyncOperation.progress >= 0.9f && UPSSaves.Prefs.IsReady && _isSplashCompleted)
                    asyncOperation.allowSceneActivation = true;

                yield return null;
            }
        }

        private IEnumerator PlaySplashAnimations()
        {
            for (var i = 0; i < _logos.Length; i++)
            {
                _logos[i].gameObject.SetActive(true);
                _logos[i].alpha = 0f;
            }

            foreach (var logo in _logos)
            {
                yield return StartCoroutine(FadeIn(logo));
                yield return new WaitForSecondsRealtime(_splashDelayDuration);
                yield return StartCoroutine(FadeOut(logo));
            }

            yield return new WaitForSecondsRealtime(0.5f);
            yield return StartCoroutine(ChangeBackgroundColor(Color.white, Color.black));
            _background.gameObject.SetActive(false);

            _isSplashCompleted = true;
        }

        private IEnumerator FadeIn(CanvasGroup logo)
        {
            var elapsedTime = 0f;
            logo.alpha = 0f;

            while (elapsedTime < _splashFadeDuration)
            {
                logo.alpha = Mathf.Clamp01(elapsedTime / _splashFadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            logo.alpha = 1f;
        }

        private IEnumerator FadeOut(CanvasGroup logo)
        {
            var elapsedTime = 0f;
            logo.alpha = 1f;

            while (elapsedTime < _splashFadeDuration)
            {
                logo.alpha = Mathf.Clamp01(1f - elapsedTime / _splashFadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            logo.alpha = 0f;
        }

        private IEnumerator ChangeBackgroundColor(Color startColor, Color endColor)
        {
            var elapsedTime = 0f;

            while (elapsedTime < _splashFadeDuration)
            {
                _background.color = Color.Lerp(startColor, endColor, elapsedTime / _splashFadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _background.color = endColor;
        }
    }
}