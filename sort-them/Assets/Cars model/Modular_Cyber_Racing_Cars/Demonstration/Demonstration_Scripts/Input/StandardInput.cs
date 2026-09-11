using UnityEngine;
using UnityEngine.InputSystem;

namespace ithappy
{
    public class StandardInput : InputBase
    {
        protected override void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                _moveInput = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                _steerInput = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                _shouldBoost = kb.leftShiftKey.isPressed;
            }
            
            base.Update();
        }
    }
}
