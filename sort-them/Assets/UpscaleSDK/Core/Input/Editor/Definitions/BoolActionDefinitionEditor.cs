#if UNITY_EDITOR
using System;
using UnityEditor;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Input.Processors.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;

namespace UpscaleSDK.Core.Input.Editor.Definitions 
{
    [CustomEditor(typeof(BoolActionDefinition))]
    public class BoolActionDefinitionEditor : InputActionDefinitionEditor
    {
        protected override Type GetSourceInterfaceType() => typeof(IInputSource<bool>);
        protected override Type GetProcessorInterfaceType() => typeof(IProcessor<bool>);
        protected override string GetHeaderLabel() => "Bool Input Action Configuration";
    }
}
#endif