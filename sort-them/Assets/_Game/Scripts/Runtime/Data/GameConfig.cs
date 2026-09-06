using UnityEngine;

namespace SortThem
{
    [CreateAssetMenu(menuName = "SortThem/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Player")]
        public float WalkSpeed = 4f;
        public float SprintSpeed = 7f;
        public float CrouchSpeed = 2f;
        public float JumpHeight = 1f;
        public float Gravity = -20f;
        public float MouseSensitivity = 0.12f;
        public float GamepadLookSpeed = 180f;
        public float StandHeight = 1.8f;
        public float PlayerRadius = 0.45f;
        public float CameraHeightSmoothTime = 0.12f;
        public float CrouchHeight = 1.0f;

        [Header("Interaction")]
        public float BaseInteractRange = 2.2f;
        public float BaseThrowDistance = 1.5f;
        public float ThrowArc = 0.4f;
        public int BaseInventoryCapacity = 5;
        public float BounceSpeed = 3f;
        public float MagnetDuration = 0.25f;

        [Header("Abilities")]
        public float AbilityCooldown = 20f;
        public float AbilityDuration = 8f;
        public float AutoCollectRadius = 15f;
        public float AutoCollectCooldown = 60f;
        public float AutoCollectDuration = 10f;
        public float AutoCollectInterval = 0.35f;
        public float AutoCollectFlightTime = 0.4f;
        public float ScrollThreshold = 0.6f;
        public float ScrollMinInterval = 0.12f;
        public float ScrollIdleReset = 0.25f;
        public float LevitateHeight = 1.4f;
        public float LevitateSpeed = 3f;

        [Header("Physics")]
        public float ActivationRadius = 3f;
        public float ActivationUpdateInterval = 0.25f;

        [Header("Level")]
        public Vector3 UnstuckCenter = new Vector3(0f, 2.5f, 0f);
        public float FloorY = 0f;
        public Vector3 LevelHalfExtents = new Vector3(20f, 6f, 17f);
        public int CollectiblesTotal = 10;

        [Header("Save")]
        public float AutosaveInterval = 60f;
    }
}
