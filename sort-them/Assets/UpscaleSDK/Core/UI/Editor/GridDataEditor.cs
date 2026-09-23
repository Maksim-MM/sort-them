#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UpscaleSDK.Core.Ui.Runtime;

namespace UpscaleSDK.Core.Ui.Editor
{
    [CustomEditor(typeof(LayerNavigation))]
    public class GridDataEditor : UnityEditor.Editor
    {
        private const string DefaultElementFieldName = "_defaultElement";
        private const string TransitionsFieldName = "_transitions";
             
        private const string North = "_north";
        private const string South = "_south";
        private const string West = "_west";
        private const string East = "_east";
        private const string Origin = "_origin";
        
        private SerializedProperty _defaultElement;
        private SerializedProperty _transitions;
        private List<string> _validationErrorMessages;
        
        private void OnEnable()
        {
            _defaultElement = serializedObject.FindProperty(DefaultElementFieldName);
            _transitions = serializedObject.FindProperty(TransitionsFieldName);
            _validationErrorMessages = new();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_defaultElement);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
            
            if (_transitions == null)
            {
                EditorGUILayout.HelpBox("Transitions property not found.", MessageType.Error);
                return;
            }
            
            for (int i = 0 ; i < _transitions.arraySize ; i++)
            {
                SerializedProperty transitionProp = _transitions.GetArrayElementAtIndex(i);
                SerializedProperty elementToTransitFromProp = transitionProp.FindPropertyRelative(Origin);
                SerializedProperty northProp = transitionProp.FindPropertyRelative(North);
                SerializedProperty southProp = transitionProp.FindPropertyRelative(South);
                SerializedProperty westProp = transitionProp.FindPropertyRelative(West);
                SerializedProperty eastProp = transitionProp.FindPropertyRelative(East);

                bool showTransition = EditorGUILayout.BeginFoldoutHeaderGroup(true, $"Transition {i + 1}");
                if (showTransition)
                {
                    DrawUIElementPropertyField(elementToTransitFromProp, "Origin");
                    DrawUIElementPropertyField(northProp, "North ↑");
                    DrawUIElementPropertyField(southProp, "South ↓");
                    DrawUIElementPropertyField(westProp,  "West ←");
                    DrawUIElementPropertyField(eastProp,  "East →");
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                
                

                if (GUILayout.Button($"Remove Transition {i + 1}"))
                {
                    _transitions.DeleteArrayElementAtIndex(i);
                }
                
                EditorGUILayout.Space();
            }
            
            if (GUILayout.Button("Add Transition"))
            {
                _transitions.arraySize++;
            }
            
            if (GUILayout.Button("Validate"))
            {
                _validationErrorMessages.Clear();
                List<UIElement> existingElements = new();
                for (int i = 0 ; i < _transitions.arraySize ; i++)
                {
                    SerializedProperty transitionProp = _transitions.GetArrayElementAtIndex(i);
                    UIElement origin = transitionProp.FindPropertyRelative(Origin).objectReferenceValue as UIElement;
                    UIElement north = transitionProp.FindPropertyRelative(North).objectReferenceValue as UIElement;
                    UIElement south = transitionProp.FindPropertyRelative(South).objectReferenceValue as UIElement;
                    UIElement west = transitionProp.FindPropertyRelative(West).objectReferenceValue as UIElement;
                    UIElement east = transitionProp.FindPropertyRelative(East).objectReferenceValue as UIElement;
                    Transition transition = new Transition(origin, north, south, west, east);
                    
                    if (existingElements.Contains(transition.Origin))
                    {
                        if (transition?.Origin != null)
                            _validationErrorMessages.Add(
                                $"Duplicate Origin assigned: {transition?.Origin.name}");
                    }
                    existingElements.Add(transition.Origin);
                    
                    string error = CheckTransitionValidity(transition, i + 1);
                    if (error != null)
                    {
                        _validationErrorMessages.Add(error);
                    }
                }
                
                if (_defaultElement.objectReferenceValue == null)
                {
                    _validationErrorMessages.Add("DefaultElement is not assigned.");
                }
            }

            if (_validationErrorMessages.Count == 0)
            {
                EditorGUILayout.HelpBox("No validation errors found.", MessageType.Info);
            }
            else
            {
                foreach (string error in _validationErrorMessages)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawUIElementPropertyField(SerializedProperty property, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            if (GUILayout.Button("Ping"))
            {
                UIElement element = property.objectReferenceValue as UIElement;
                if (element != null)
                {
                    EditorGUIUtility.PingObject(element);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private string CheckTransitionValidity(Transition transition, int index)
        {
            StringBuilder sb = new StringBuilder();
            bool hasIssue = false;
            if (transition.Origin == null)
            {
                sb.AppendLine($"[Transition {index}] Origin is null.");
                hasIssue = true;
            }
            
            if (transition.North == null && transition.South == null && transition.West == null && transition.East == null)
            {
                sb.AppendLine($"[Transition {index}] All directional transitions are null.");
                hasIssue = true;
            }
            
            if (transition.Origin == transition.North ||
                transition.Origin == transition.South ||
                transition.Origin == transition.West ||
                    transition.Origin == transition.East)
            {
                _validationErrorMessages.Add(
                    $"Transition {index}: Origin cannot be the same as any of its directional transitions.");
            }

            if (hasIssue) return sb.ToString();
            
            return null;
        }
    }
}
#endif