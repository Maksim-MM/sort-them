#if UNITY_EDITOR
using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UpscaleSDK.Core.Input.Editor.Definitions
{
    public abstract class InputActionDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _sourcesProp;
        private SerializedProperty _processorsProp;
       // private SerializedProperty _actionNameProp;
       // private SerializedProperty _playerIdProp;
        private ReorderableList _sourcesList;
        private ReorderableList _processorsList;
        protected abstract Type GetSourceInterfaceType();
        protected abstract Type GetProcessorInterfaceType();
        protected abstract string GetHeaderLabel();

        private void OnEnable()
        {
            _sourcesProp = serializedObject.FindProperty("sources");
            _processorsProp = serializedObject.FindProperty("processors");
            //_actionNameProp = serializedObject.FindProperty("actionName");
           // _playerIdProp = serializedObject.FindProperty("playerId");
            if (_sourcesProp != null)
                _sourcesList = CreateList(_sourcesProp, "Input Sources", GetSourceInterfaceType());
            if (_processorsProp != null)
                _processorsList = CreateList(_processorsProp, "Processors", GetProcessorInterfaceType());
        }

        public override void OnInspectorGUI()
        {
            if (_sourcesProp == null || _processorsProp == null)
            {
                EditorGUILayout.HelpBox("Serialized Properties not found! Check field names.", MessageType.Error);
                return;
            }

            serializedObject.Update();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(GetHeaderLabel(), EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            //EditorGUILayout.Space(5);
            //if (_actionNameProp != null) EditorGUILayout.PropertyField(_actionNameProp);
            //if (_playerIdProp != null) EditorGUILayout.PropertyField(_playerIdProp);
            
            EditorGUILayout.Space(10);
            _sourcesList.DoLayoutList();
            EditorGUILayout.Space(5);
            _processorsList.DoLayoutList();
            
            // Check for duplicate processors
            var duplicateProcessors = GetDuplicates(_processorsProp);
            if (duplicateProcessors.Any())
            {
                EditorGUILayout.Space(5);
                string processorNames = string.Join(", ", duplicateProcessors.Select(t => GetFriendlyTypeName(t.Name)));
                EditorGUILayout.HelpBox($"Duplicate processors detected: {processorNames}", MessageType.Warning);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private System.Collections.Generic.List<Type> GetDuplicates(SerializedProperty arrayProperty)
        {
            var typeCounts = new System.Collections.Generic.Dictionary<Type, int>();
            
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue != null)
                {
                    var type = element.managedReferenceValue.GetType();
                    if (!typeCounts.TryAdd(type, 1))
                        typeCounts[type]++;
                }
            }
            
            return typeCounts.Where(kvp => kvp.Value > 1).Select(kvp => kvp.Key).ToList();
        }

        private ReorderableList CreateList(SerializedProperty property, string headerName, Type baseType)
        {
            var list = new ReorderableList(serializedObject, property, true, true, true, true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, headerName, EditorStyles.boldLabel);
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = property.GetArrayElementAtIndex(index);
                rect.x += EditorGUIUtility.standardVerticalSpacing * 4;
                rect.width -= EditorGUIUtility.standardVerticalSpacing * 4;
                if (element.managedReferenceValue == null)
                {
                    var errorStyle = new GUIStyle { normal = { textColor = Color.red } };
                    EditorGUI.LabelField(rect, $"Missing type", errorStyle);
                    return;
                }

                string typeName = GetFriendlyTypeName(element.managedReferenceValue.GetType().Name);
                EditorGUI.PropertyField(rect, element, new GUIContent(typeName), true);
            };
            list.elementHeightCallback = index =>
            {
                var element = property.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true);
            };
            list.onAddDropdownCallback = (rect, l) =>
            {
                var menu = new GenericMenu();
                var types = TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract && !t.IsInterface);
                foreach (var type in types)
                {
                    menu.AddItem(new GUIContent(GetFriendlyTypeName(type.Name)), false, () => AddElement(l, type));
                }

                menu.ShowAsContext();
            };
            return list;
        }

        private void AddElement(ReorderableList list, Type type)
        {
            serializedObject.Update();
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            object instance = FormatterServices.GetUninitializedObject(type);
            element.managedReferenceValue = instance;
            serializedObject.ApplyModifiedProperties();
        }

        private string GetFriendlyTypeName(string typeName)
        {
            return typeName.Replace("Source", "").Replace("Processor", "").Replace("Bool", "").Replace("Vector2", "")
                .Replace("Float", "").Trim();
        }
    }
}
#endif