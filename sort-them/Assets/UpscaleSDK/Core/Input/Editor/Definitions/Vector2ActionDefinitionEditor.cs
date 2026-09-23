#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Input.Processors.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;

namespace UpscaleSDK.Core.Input.Editor.Definitions
{
    [CustomEditor(typeof(Vector2ActionDefinition))]
    public class Vector2ActionDefinitionEditor : InputActionDefinitionEditor
    {
        protected override Type GetSourceInterfaceType() => typeof(IInputSource<Vector2>);
        protected override Type GetProcessorInterfaceType() => typeof(IProcessor<Vector2>);
        protected override string GetHeaderLabel() => "Vector2 Input Action Configuration";
    }
}
#endif