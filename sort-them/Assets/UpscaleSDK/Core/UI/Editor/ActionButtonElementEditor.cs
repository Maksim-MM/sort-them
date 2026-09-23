#if UNITY_EDITOR
using UnityEditor;
using UpscaleSDK.Core.Ui.Runtime;

namespace UpscaleSDK.Core.Ui.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ActionButtonElement))]
    public class ActionButtonElementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            if (serializedObject.FindProperty("_showIcon").boolValue)
            {
                
                if (serializedObject.FindProperty("_iconImage").objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Icon Image not set. Assign an Image component for the icon.", MessageType.Error);
                }
            }
            
            if (serializedObject.FindProperty("_definition").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Definition not set.", MessageType.Error);
            }
            
            if (serializedObject.FindProperty("_layer").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Layer not set.", MessageType.Error);
            }
            
            if (serializedObject.FindProperty("_button").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Button not set.", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif