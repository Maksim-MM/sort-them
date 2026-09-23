#if UNITY_EDITOR
using System;
using UnityEditor;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Input.Processors.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;

namespace UpscaleSDK.Core.Input.Editor.Definitions
{
    [CustomEditor(typeof(FloatActionDefinition))]
    public class FloatActionDefinitionEditor : InputActionDefinitionEditor
    {
        protected override Type GetSourceInterfaceType() => typeof(IInputSource<float>);
        protected override Type GetProcessorInterfaceType() => typeof(IProcessor<float>);
        protected override string GetHeaderLabel() => "Float Input Action Configuration";
    }
}
#endif