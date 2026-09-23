using System;
using UnityEngine;
using UpscaleSDK.Core.Saves.ErrorHandling;

namespace Plugins.UpscaleSDK.Saves.Runtime.ErrorHandling
{
    public class PopupErrorHandler : MonoBehaviour
    {
        [SerializeField] private GameObject errorPopupPrefab;
        
        private void OnEnable()
        {
            ErrorHandler.OnError += ShowErrorPopup;
        }

        private void OnDisable()
        {
            ErrorHandler.OnError -= ShowErrorPopup;
        }

        private void ShowErrorPopup(string errorMessage)
        {
            if (errorPopupPrefab != null)
            {
                GameObject popupInstance = Instantiate(errorPopupPrefab);
                ErrorPopup popupComponent = popupInstance.GetComponent<ErrorPopup>();
                if (popupComponent != null)
                {
                    popupComponent.SetErrorMessage(errorMessage);
                }
                else
                {
                    Debug.LogError("ErrorPopup component not found on the prefab.");
                }
            }
            else
            {
                Debug.LogError("Error popup prefab is not assigned.");
            }
        }
    }
}