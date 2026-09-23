using UnityEngine;

namespace UpscaleSDK.Core.Utils
{ 
    /// <summary>
    /// Represents a module enum.
    /// </summary>
    public enum Module
    {
        /// <summary>
        /// Saves.
        /// </summary>
        Saves,
        /// <summary>
        /// Input.
        /// </summary>
        Input,
        /// <summary>
        /// Ui.
        /// </summary>
        UI,
        /// <summary>
        /// Core.
        /// </summary>
        Core
    }

    /// <summary>
    /// Represents a log type enum.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Info.
        /// </summary>
        Info,
        /// <summary>
        /// Dev.
        /// </summary>
        Dev,
        /// <summary>
        /// Error.
        /// </summary>
        Error
    }
    
    /// <summary>
    /// Represents a ups logger class.
    /// </summary>
    public static class UPSLogger
    {
        private const string Prefix = "[UPS-";
        private const bool EnableDevLogs = true;

        private static void Log(Module module, string message, LogType logType = LogType.Info)
        {
            if (logType == LogType.Dev)
                if (!EnableDevLogs)
                    return;
            if (logType == LogType.Error)
            {
                Debug.LogError($"{Prefix}{module.ToString().ToUpper()}] {message}");
                return;
            }
            Debug.Log($"{Prefix}{module.ToString().ToUpper()}] {message}");
        }

        /// <summary>
        /// Saves info log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void SavesInfoLog(string message)
        {
            Log(Module.Saves, message, LogType.Info);
        }
        
        /// <summary>
        /// Saves dev log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void SavesDevLog(string message)
        {
            Log(Module.Saves, message, LogType.Dev);
        }
        
        /// <summary>
        /// Saves error log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void SavesErrorLog(string message)
        {
            Log(Module.Saves, message, LogType.Error);
        }
        
        /// <summary>
        /// Input info log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void InputInfoLog(string message)
        {
            Log(Module.Input, message, LogType.Info);
        }
        
        /// <summary>
        /// Input dev log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void InputDevLog(string message)
        {
            Log(Module.Input, message, LogType.Dev);
        }
        
        /// <summary>
        /// Core info log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void CoreInfoLog(string message)
        {
            Log(Module.Core, message, LogType.Info);
        }
        
        /// <summary>
        /// Core dev log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void CoreDevLog(string message)
        {
            Log(Module.Core, message, LogType.Dev);
        }
        
        /// <summary>
        /// Core error log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void CoreErrorLog(string message)
        {
            Log(Module.Core, message, LogType.Error);
        }
        
        /// <summary>
        /// Ui info log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void UiInfoLog(string message)
        {
            Log(Module.UI, message, LogType.Info);
        }
        
        /// <summary>
        /// Ui dev log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void UiDevLog(string message)
        {
            Log(Module.UI, message, LogType.Dev);
        }
        
        /// <summary>
        /// Ui error log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void UiErrorLog(string message)
        {
            Log(Module.UI, message, LogType.Error);
        }
    }
}