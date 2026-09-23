#if UNITY_EDITOR
using UnityEditor;
using UpscaleSDK.Core.Ui.Runtime;

namespace UpscaleSDK.Core.Ui.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ActionToggleElement))]
    public class ActionToggleElementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (serializedObject.FindProperty("_definition").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Definition not set.", MessageType.Error);
            }

            if (serializedObject.FindProperty("_layer").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Layer not set.", MessageType.Error);
            }

            if (serializedObject.FindProperty("_toggle").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Toggle not set.", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
