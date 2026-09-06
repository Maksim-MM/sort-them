using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SortThem.Editor
{
    public static class InputSetup
    {
        [MenuItem("SortThem/2. Input Actions")]
        public static InputActionAsset Create()
        {
            EditorAssets.EnsureFolder(Paths.Input);
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "SortThemInput";
            var map = asset.AddActionMap("Player");

            var move = map.AddAction("Move", InputActionType.Value);
            move.expectedControlType = "Vector2";
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", "KeyboardMouse")
                .With("Down", "<Keyboard>/s", "KeyboardMouse")
                .With("Left", "<Keyboard>/a", "KeyboardMouse")
                .With("Right", "<Keyboard>/d", "KeyboardMouse");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", "KeyboardMouse")
                .With("Down", "<Keyboard>/downArrow", "KeyboardMouse")
                .With("Left", "<Keyboard>/leftArrow", "KeyboardMouse")
                .With("Right", "<Keyboard>/rightArrow", "KeyboardMouse");
            move.AddBinding("<Gamepad>/leftStick").WithGroup("Gamepad");

            var look = map.AddAction("Look", InputActionType.Value);
            look.expectedControlType = "Vector2";
            look.AddBinding("<Mouse>/delta").WithGroup("KeyboardMouse");
            look.AddBinding("<Gamepad>/rightStick").WithGroup("Gamepad");

            Button(map, "Interact", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            Button(map, "PlaceOrThrow", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            var scroll = map.AddAction("ScrollItems", InputActionType.Value);
            scroll.expectedControlType = "Axis";
            scroll.AddBinding("<Mouse>/scroll/y").WithGroup("KeyboardMouse");
            var next = map.AddAction("NextItem", InputActionType.Button);
            next.AddBinding("<Gamepad>/rightShoulder").WithGroup("Gamepad");
            var prev = map.AddAction("PrevItem", InputActionType.Button);
            prev.AddBinding("<Gamepad>/leftShoulder").WithGroup("Gamepad");
            Button(map, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Button(map, "Crouch", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            Button(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Button(map, "Ability1", "<Keyboard>/1", "<Gamepad>/dpad/up");
            Button(map, "Ability2", "<Keyboard>/2", "<Gamepad>/dpad/left");
            Button(map, "Ability3", "<Keyboard>/3", "<Gamepad>/dpad/right");
            Button(map, "Pause", "<Keyboard>/escape", "<Gamepad>/start");

            var ui = asset.AddActionMap("UI");
            var navigate = ui.AddAction("Navigate", InputActionType.Value);
            navigate.expectedControlType = "Vector2";
            navigate.AddBinding("<Gamepad>/dpad").WithGroup("Gamepad");
            navigate.AddBinding("<Gamepad>/leftStick").WithGroup("Gamepad");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", "KeyboardMouse")
                .With("Down", "<Keyboard>/downArrow", "KeyboardMouse")
                .With("Left", "<Keyboard>/leftArrow", "KeyboardMouse")
                .With("Right", "<Keyboard>/rightArrow", "KeyboardMouse");
            Button(ui, "Submit", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            var cancel = ui.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Gamepad>/buttonEast").WithGroup("Gamepad");

            asset.AddControlScheme("KeyboardMouse").WithRequiredDevice("<Keyboard>").WithRequiredDevice("<Mouse>");
            asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");

            File.WriteAllText(Paths.InputAsset, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(Paths.InputAsset, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Paths.InputAsset);
            Debug.Log("SortThem: input actions written, actions=" + (loaded != null ? loaded.FindActionMap("Player").actions.Count : -1));
            return loaded;
        }

        static void Button(InputActionMap map, string name, string kbm, string pad)
        {
            var a = map.AddAction(name, InputActionType.Button);
            a.AddBinding(kbm).WithGroup("KeyboardMouse");
            a.AddBinding(pad).WithGroup("Gamepad");
        }
    }
}
