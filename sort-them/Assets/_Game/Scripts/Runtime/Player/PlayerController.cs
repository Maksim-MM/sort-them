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
        float _eyeY, _eyeVel, _eyeTilt;
        bool _camInit;
        bool _crouching, _wantCrouch;
        float _stepTimer;
        Camera _cam;
        float _baseFov = 70f, _fovVelocity;

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
            _eyeY = GameManager.I.Config.StandHeight - 0.15f;
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || CameraPivot == null) return;
            float target = transform.position.y;
            if (!_camInit) { _camY = target; _camInit = true; }
            float smooth = _cc.isGrounded ? gm.Config.CameraHeightSmoothTime : gm.Config.CameraHeightSmoothTime * 0.25f;
            _camY = Mathf.SmoothDamp(_camY, target, ref _camYVelocity, smooth);
            _camY = Mathf.Clamp(_camY, target - 0.4f, target + 0.4f);
            var p = transform.position;
            CameraPivot.position = new Vector3(p.x, _camY + _eyeY, p.z);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready || _move == null) return;
            var cfg = gm.Config;
            bool blocked = gm.UiBlocking;

            bool touch = TouchInput.Active;
            Cursor.lockState = blocked || touch ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked || touch;

            if (!blocked)
            {
                Vector2 look = touch ? Vector2.zero : _look.ReadValue<Vector2>();
                bool gamepad = _look.activeControl != null && _look.activeControl.device is Gamepad;
                look *= gamepad ? cfg.GamepadLookSpeed * Time.deltaTime : cfg.MouseSensitivity;
                if (touch) look += TouchInput.ConsumeLook();
                look.x *= Settings.SensitivityX;
                look.y *= Settings.SensitivityY;
                _yaw += look.x;
                _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            if (!blocked && (_crouch.WasPressedThisFrame() || TouchInput.Consume(TouchButton.Crouch)) && gm.Upgrades.Has(UpgradeKind.Crouch)) _wantCrouch = !_wantCrouch;
            bool wantCrouch = _wantCrouch && gm.Upgrades.Has(UpgradeKind.Crouch);
            if (wantCrouch != _crouching)
            {
                if (wantCrouch || CanStand())
                {
                    _crouching = wantCrouch;
                    SetHeight(_crouching ? cfg.CrouchHeight : cfg.StandHeight);
                    Sfx.Play(_crouching ? cfg.CrouchDownClip : cfg.CrouchUpClip, transform.position, 1f, Random.Range(0.95f, 1.05f));
                }
            }
            float crouchF = UpdateEye(cfg);
            if (CameraPivot != null) CameraPivot.localRotation = Quaternion.Euler(_pitch + _eyeTilt, 0f, 0f);

            Vector2 moveInput = blocked ? Vector2.zero : Vector2.ClampMagnitude(_move.ReadValue<Vector2>() + TouchInput.Move, 1f);
            if (TouchInput.SprintToggled && !gm.Upgrades.Has(UpgradeKind.Sprint)) TouchInput.SprintToggled = false;
            bool sprint = !blocked && (_sprint.IsPressed() || TouchInput.SprintToggled) && gm.Upgrades.Has(UpgradeKind.Sprint) && !_crouching;
            float speed = Mathf.Lerp(sprint ? cfg.SprintSpeed : cfg.WalkSpeed, cfg.CrouchSpeed, crouchF);
            Vector3 dir = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            if (_cc.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (!blocked && (_jump.WasPressedThisFrame() || TouchInput.Consume(TouchButton.Jump)) && !_crouching)
                    _verticalVelocity = Mathf.Sqrt(2f * -cfg.Gravity * cfg.JumpHeight);
            }
            _verticalVelocity += cfg.Gravity * Time.deltaTime;

            Vector3 velocity = dir * speed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
            Footsteps(cfg, dir.sqrMagnitude > 0.01f && _cc.isGrounded, sprint);
            UpdateFov(cfg, sprint && dir.sqrMagnitude > 0.01f);
        }

        float UpdateEye(GameConfig cfg)
        {
            float standEye = cfg.StandHeight - 0.15f;
            float crouchEye = cfg.CrouchHeight - 0.15f;
            float span = Mathf.Max(0.01f, standEye - crouchEye);
            float target = _crouching ? crouchEye : standEye;
            float zeta = 1f, omega;
            if (_crouching && cfg.CrouchDip > 0f)
            {
                float ln = Mathf.Log(Mathf.Clamp(cfg.CrouchDip / span, 0.001f, 0.5f));
                zeta = -ln / Mathf.Sqrt(Mathf.PI * Mathf.PI + ln * ln);
                omega = (Mathf.PI - Mathf.Acos(zeta)) / (Mathf.Sqrt(1f - zeta * zeta) * Mathf.Max(0.01f, cfg.CrouchDownTime));
            }
            else omega = 4.74f / Mathf.Max(0.01f, _crouching ? cfg.CrouchDownTime : cfg.CrouchUpTime);
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.008f));
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                _eyeVel += (omega * omega * (target - _eyeY) - 2f * zeta * omega * _eyeVel) * h;
                _eyeY += _eyeVel * h;
            }
            if (Mathf.Abs(_eyeY - target) < 0.0005f && Mathf.Abs(_eyeVel) < 0.005f) { _eyeY = target; _eyeVel = 0f; }
            _eyeTilt = cfg.CrouchTilt * Mathf.Clamp(-_eyeVel * cfg.CrouchDownTime / span, -1f, 1f);
            return Mathf.Clamp01((standEye - _eyeY) / span);
        }

        void UpdateFov(GameConfig cfg, bool sprinting)
        {
            if (_cam == null)
            {
                _cam = CameraPivot != null ? CameraPivot.GetComponentInChildren<Camera>() : null;
                if (_cam == null) return;
                _baseFov = _cam.fieldOfView;
            }
            float target = _baseFov + (sprinting ? cfg.SprintFovBoost : 0f);
            _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, target, ref _fovVelocity, cfg.FovSmoothTime);
        }

        void Footsteps(GameConfig cfg, bool moving, bool sprint)
        {
            if (!moving) { _stepTimer = 0.1f; return; }
            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            float interval = sprint ? cfg.FootstepRunInterval : cfg.FootstepWalkInterval;
            if (_crouching) interval *= 1.4f;
            _stepTimer = interval;
            var clip = sprint ? Sfx.Pick(cfg.FootstepRunClips, cfg.FootstepRunClip) : Sfx.Pick(cfg.FootstepWalkClips, cfg.FootstepWalkClip);
            float pitch = Random.Range(0.92f, 1.08f);
            if (sprint && cfg.FootstepRunClips.Length == 0 && cfg.FootstepWalkClips.Length > 0) { clip = Sfx.Pick(cfg.FootstepWalkClips, cfg.FootstepWalkClip); pitch *= 1.1f; }
            Sfx.Play(clip, transform.position, _crouching ? 0.5f : 1f, pitch);
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
