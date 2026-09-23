using System;
using Plugins.UpscaleSDK.PS5.Runtime.App;
using Plugins.UpscaleSDK.PS5.Runtime.Common;
#if UNITY_PS5
using Unity.PSN.PS5.UDS;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.Users.Activities
{
    /// <summary>
    /// Represents a ps5 activities class.
    /// </summary>
    public class Ps5Activities
    {
        /// <summary>
        /// Occurs when activity started.
        /// </summary>
        public event Action<string> OnActivityStarted;
        /// <summary>
        /// Occurs when activity finished.
        /// </summary>
        public event Action<string, ActivityResult> OnActivityFinished;
        
        private const string ActivityStartTag = "activityStart";
        private const string ActivityEndTag = "activityEnd";
        private const string ActivityIdTag = "activityId";
        private const string OutcomeTag = "outcome";
        
        private AppData _appData;
        private UserData _userData;
        
        private static Ps5Activities _instance;
        
        public Ps5Activities(AppData appData, UserData userData)
        {
            _appData = appData;
            _userData = userData;
            _instance = this;
        }

        /// <summary>
        /// Start activity.
        /// </summary>
        /// <param name="id">The id.</param>
        public static void StartActivity(string id)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.StartActivity(id);
        }

        /// <summary>
        /// Finish activity.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="result">The result.</param>
        public static void FinishActivity(string id,  ActivityResult result = ActivityResult.Completed)
        { 
            if (_instance == null)
            {
                return;
            }
            
            _instance.FinishActivity(id, result);
        }

        /// <summary>
        /// Start activity.
        /// </summary>
        /// <param name="activityId">The activity id.</param>
        /// <param name="callback">The callback.</param>
        public void StartActivity(string activityId, Action callback = null)
        {
#if UNITY_PS5
            if (_appData.CanUseSocial == false) return;

            UniversalDataSystem.UDSEvent eventData = new();
            eventData.Create(ActivityStartTag);
            eventData.Properties.Set(ActivityIdTag, activityId);
            
            UniversalDataSystem.PostEventRequest request = new()
            {
                UserId = _userData.PSUser.userId,
                EventData = eventData
            };
            
            var requestOperation = new Unity.PSN.PS5.Aysnc.AsyncRequest<UniversalDataSystem.PostEventRequest>(request).ContinueWith((
                antecedent) =>
            {
                if (antecedent.IsSuccess())
                {
                    callback?.Invoke();
                    OnActivityStarted?.Invoke(activityId);
                }
            });
            
            UniversalDataSystem.Schedule(requestOperation);
#endif
        }
        
        /// <summary>
        /// Finish activity.
        /// </summary>
        /// <param name="activityId">The activity id.</param>
        /// <param name="result">The result.</param>
        /// <param name="callback">The callback.</param>
        public void FinishActivity(string activityId, ActivityResult result = ActivityResult.Completed, Action callback = null)
        {
#if UNITY_PS5
            if (_appData.CanUseSocial == false) return;

            UniversalDataSystem.UDSEvent eventData = new();
            eventData.Create(ActivityEndTag);
            eventData.Properties.Set(ActivityIdTag, activityId);
            eventData.Properties.Set(OutcomeTag, result.ToString().ToLower());
            
            UniversalDataSystem.PostEventRequest request = new()
            {
                UserId = _userData.PSUser.userId,
                EventData = eventData
            };
            
            var requestOperation = new Unity.PSN.PS5.Aysnc.AsyncRequest<UniversalDataSystem.PostEventRequest>(request).ContinueWith((
                antecedent) =>
            {
                if (antecedent.IsSuccess())
                {
                    callback?.Invoke();
                    OnActivityFinished?.Invoke(activityId, result);
                }
            });
            
            UniversalDataSystem.Schedule(requestOperation);
#endif
        }
    }
}