using UnityEngine;
using UnityEngine.InputSystem;

namespace SortThem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform CameraPivot;

        CharacterController _cc;
        InputAction _move, _look, _jump, _sprint, _crouch;
        float _yaw, _pitch, _verticalVelocity, _camY, _camYVelocity;
        bool _camInit;
        bool _crouching, _wantCrouch;
        float _stepTimer;

        public bool IsCrouching => _crouching;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _yaw = transform.eulerAngles.y;
        }

        void Start()
        {
            var map = GameManager.I.InputAsset.FindActionMap("Player", true);
            _move = map.FindAction("Move", true);
            _look = map.FindAction("Look", true);
            _jump = map.FindAction("Jump", true);
            _sprint = map.FindAction("Sprint", true);
            _crouch = map.FindAction("Crouch", true);
            _cc.radius = GameManager.I.Config.PlayerRadius;
            SetHeight(GameManager.I.Config.StandHeight);
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || CameraPivot == null) return;
            float target = transform.position.y + _cc.height - 0.15f;
            if (!_camInit) { _camY = target; _camInit = true; }
            float smooth = _cc.isGrounded ? gm.Config.CameraHeightSmoothTime : gm.Config.CameraHeightSmoothTime * 0.25f;
            _camY = Mathf.SmoothDamp(_camY, target, ref _camYVelocity, smooth);
            _camY = Mathf.Clamp(_camY, target - 0.4f, target + 0.4f);
            var p = transform.position;
            CameraPivot.position = new Vector3(p.x, _camY, p.z);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready || _move == null) return;
            var cfg = gm.Config;
            bool blocked = gm.UiBlocking;

            Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked;

            if (!blocked)
            {
                Vector2 look = _look.ReadValue<Vector2>();
                bool gamepad = _look.activeControl != null && _look.activeControl.device is Gamepad;
                look *= gamepad ? cfg.GamepadLookSpeed * Time.deltaTime : cfg.MouseSensitivity;
                look.x *= Settings.SensitivityX;
                look.y *= Settings.SensitivityY;
                _yaw += look.x;
                _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (CameraPivot != null) CameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            if (!blocked && _crouch.WasPressedThisFrame() && gm.Upgrades.Has(UpgradeKind.Crouch)) _wantCrouch = !_wantCrouch;
            bool wantCrouch = _wantCrouch && gm.Upgrades.Has(UpgradeKind.Crouch);
            if (wantCrouch != _crouching)
            {
                if (wantCrouch || CanStand())
                {
                    _crouching = wantCrouch;
                    SetHeight(_crouching ? cfg.CrouchHeight : cfg.StandHeight);
                }
            }

            Vector2 moveInput = blocked ? Vector2.zero : _move.ReadValue<Vector2>();
            bool sprint = !blocked && _sprint.IsPressed() && gm.Upgrades.Has(UpgradeKind.Sprint) && !_crouching;
            float speed = _crouching ? cfg.CrouchSpeed : sprint ? cfg.SprintSpeed : cfg.WalkSpeed;
            Vector3 dir = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            if (_cc.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (!blocked && _jump.WasPressedThisFrame() && !_crouching)
                    _verticalVelocity = Mathf.Sqrt(2f * -cfg.Gravity * cfg.JumpHeight);
            }
            _verticalVelocity += cfg.Gravity * Time.deltaTime;

            Vector3 velocity = dir * speed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
            Footsteps(cfg, dir.sqrMagnitude > 0.01f && _cc.isGrounded, sprint);
        }

        void Footsteps(GameConfig cfg, bool moving, bool sprint)
        {
            if (!moving) { _stepTimer = 0.1f; return; }
            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            float interval = sprint ? cfg.FootstepRunInterval : cfg.FootstepWalkInterval;
            if (_crouching) interval *= 1.4f;
            _stepTimer = interval;
            var clip = sprint ? cfg.FootstepRunClip : cfg.FootstepWalkClip;
            Sfx.Play(clip, transform.position, _crouching ? 0.5f : 1f, Random.Range(0.92f, 1.08f));
        }

        bool CanStand()
        {
            var cfg = GameManager.I.Config;
            float extra = cfg.StandHeight - _cc.height;
            Vector3 top = transform.position + Vector3.up * (_cc.height - _cc.radius);
            return !Physics.SphereCast(top, _cc.radius * 0.9f, Vector3.up, out _, extra + 0.05f, Layers.InteractMask, QueryTriggerInteraction.Ignore);
        }

        void SetHeight(float height)
        {
            _cc.height = height;
            _cc.center = new Vector3(0f, height * 0.5f, 0f);
            if (CameraPivot != null) CameraPivot.localPosition = new Vector3(0f, height - 0.15f, 0f);
        }

        public void Teleport(Vector3 position)
        {
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = true;
            _camInit = false;
        }
    }
}
