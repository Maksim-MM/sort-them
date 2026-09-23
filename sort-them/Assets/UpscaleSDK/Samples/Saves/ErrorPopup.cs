using System.Collections;
using UnityEngine;

namespace Plugins.UpscaleSDK.Saves.Runtime.ErrorHandling
{
    public class ErrorPopup : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI errorMessageText;
        [SerializeField] private float _lifeTime = 4f;
        
        private void Awake()
        {
            StartCoroutine(AutoCloseAfterDelay());
        }

        public void SetErrorMessage(string errorMessage)
        {
            if (errorMessageText != null)
            {
                errorMessageText.text = errorMessage;
            }
        }

        public void ClosePopup()
        {
            Destroy(gameObject);
        }
        
        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(_lifeTime);
            ClosePopup();
        }
    }
}