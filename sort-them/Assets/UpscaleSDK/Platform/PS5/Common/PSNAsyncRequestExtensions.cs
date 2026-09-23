#if UNITY_PS5
using Unity.PSN.PS5;
using Unity.PSN.PS5.Aysnc;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.Common
{
    /// <summary>
    /// Represents a psn async request extensions class.
    /// </summary>
    public static class PSNAsyncRequestExtensions
    {
        #if UNITY_PS5
        /// <summary>
        /// Indicates whether success.
        /// </summary>
        /// <param name="request">The request.</param>
        public static bool IsSuccess<T>(this AsyncRequest<T> request) where T : Request
        {
            if (request == null)
                return false;
            if (request.Request.Result.apiResult == APIResultTypes.Error) return false;
            return true;
        }
        #endif
    }
}