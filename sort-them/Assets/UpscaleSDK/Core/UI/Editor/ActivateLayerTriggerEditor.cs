#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UpscaleSDK.Core.Ui.Runtime;
using UpscaleSDK.Core.Ui.Runtime.Enums;

namespace UpscaleSDK.Core.Ui.Editor
{
    [CustomEditor(typeof(ActivateLayerTrigger))]
    public class ActivateLayerTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _triggerType;
        private SerializedProperty _findType;
        private SerializedProperty _layerReference;
        private SerializedProperty _layerKey;

        private void OnEnable()
        {
            _triggerType = serializedObject.FindProperty("_triggerType");
            _findType = serializedObject.FindProperty("_findType");
            _layerReference = serializedObject.FindProperty("_layerReference");
            _layerKey = serializedObject.FindProperty("_layerKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_triggerType);

            EditorGUILayout.PropertyField(_findType, new GUIContent("Find Type"));
            
            FindType findType = (FindType)_findType.enumValueIndex;

            if (findType == FindType.Reference)
            {
                EditorGUILayout.PropertyField(_layerReference, new GUIContent("Layer Reference"));
                
                if (_layerReference.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Layer Reference is not assigned!", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(_layerKey, new GUIContent("Layer Key"));
                
                if (string.IsNullOrEmpty(_layerKey.stringValue))
                {
                    EditorGUILayout.HelpBox("Layer Key is empty!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Make sure a Layer with this key exists and registered", MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif