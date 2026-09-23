using UnityEngine;

namespace UpscaleSDK.Core.Ui.Samples.Base
{
    public class DemoWindow : MonoBehaviour
    {
        public void Close()
        {
            gameObject.SetActive(false);
        }
    
        public void Open()
        {
            gameObject.SetActive(true);
        }
    }
}
