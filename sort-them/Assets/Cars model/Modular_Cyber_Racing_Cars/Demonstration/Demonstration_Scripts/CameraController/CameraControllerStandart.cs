using UnityEngine;
using UnityEngine.InputSystem;

namespace ithappy
{
    public class CameraControllerStandart : CameraControllerBase
    {
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame)
            {
                _mode = CameraMode.TopDown;
            }
            if (kb.digit2Key.wasPressedThisFrame)
            {
                _mode = CameraMode.ThirdPerson;
            }
        }
    }
}
