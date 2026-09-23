#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UpscaleSDK.Core.Input.Glyphs;
using UpscaleSDK.Core.Input.Glyphs.Enums;

namespace UpscaleSDK.Core.Input.Editor.Glyphs
{
    [CustomEditor(typeof(GamepadIconsSet))]
    public class GamepadIconsSetEditor : UnityEditor.Editor
    {
        private SerializedProperty iconsProperty;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            iconsProperty = serializedObject.FindProperty("icons");
            GenerateAllGlyphs();
            //Debug.Log("Gamepad Icons Set");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < iconsProperty.arraySize; i++)
            {
                SerializedProperty item = iconsProperty.GetArrayElementAtIndex(i);
                SerializedProperty glyphProp = item.FindPropertyRelative("Glyph");
                SerializedProperty spriteProp = item.FindPropertyRelative("Sprite");

                GamepadGlyph glyph = (GamepadGlyph)glyphProp.enumValueIndex;

                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                
                EditorGUILayout.LabelField(glyph.ToString(), GUILayout.Width(150));
                
                Sprite sprite = spriteProp.objectReferenceValue as Sprite;
                Rect previewRect = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40));
                
                if (sprite != null)
                {
                    GUI.DrawTexture(previewRect, sprite.texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                }
                
                EditorGUILayout.PropertyField(spriteProp, GUIContent.none);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateAllGlyphs()
        {
            var existingGlyphs = new HashSet<GamepadGlyph>();

            for (int i = iconsProperty.arraySize - 1; i >= 0; i--)
            {
                var item = iconsProperty.GetArrayElementAtIndex(i);
                var glyphProp = item.FindPropertyRelative("Glyph");

                if (glyphProp.enumValueIndex < 0)
                {
                    iconsProperty.DeleteArrayElementAtIndex(i);
                    continue;
                }

                var glyph = (GamepadGlyph)glyphProp.enumValueIndex;
                if (!existingGlyphs.Add(glyph))
                {
                    iconsProperty.DeleteArrayElementAtIndex(i);
                }
            }

            foreach (var glyph in Enum.GetValues(typeof(GamepadGlyph)).Cast<GamepadGlyph>())
            {
                if (existingGlyphs.Add(glyph))
                {
                    iconsProperty.arraySize++;
                    var newItem = iconsProperty.GetArrayElementAtIndex(iconsProperty.arraySize - 1);
                    newItem.FindPropertyRelative("Glyph").enumValueIndex = (int)glyph;
                    newItem.FindPropertyRelative("Sprite").objectReferenceValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif