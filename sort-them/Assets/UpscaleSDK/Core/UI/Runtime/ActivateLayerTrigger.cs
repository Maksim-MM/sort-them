using UnityEngine;
using UpscaleSDK.Core.Ui.Runtime.Enums;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a activate layer trigger class.
    /// </summary>
    public class ActivateLayerTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private TriggerType _triggerType = TriggerType.OnEnable;
        
        [Header("Layer Target")]
        [SerializeField] private FindType _findType = FindType.Reference;
        [SerializeField] private Layer _layerReference;
        [SerializeField] private string _layerKey;
        
        private void Awake()
        {
            if (_triggerType == TriggerType.Awake)
                ExecuteTrigger();
        }
        
        private void Start()
        {
            if (_triggerType == TriggerType.Start)
                ExecuteTrigger();
        }
        
        private void OnEnable()
        {
            if (_triggerType == TriggerType.OnEnable)
                ExecuteTrigger();
        }
        
        private void OnDisable()
        {
            if (_triggerType == TriggerType.OnDisable)
                ExecuteTrigger();
        }
        
        private void OnDestroy()
        {
            if (_triggerType == TriggerType.OnDestroy)
                ExecuteTrigger();
        }
        
        /// <summary>
        /// Execute trigger.
        /// </summary>
        public void ExecuteTrigger()
        {
            if (_findType == FindType.Key)
            { 
                UILayersManager.ActivateLayer(_layerKey);
            }
            else if (_findType == FindType.Reference)
            {
                UILayersManager.ActivateLayer(_layerReference);
            }
        }
    }
}