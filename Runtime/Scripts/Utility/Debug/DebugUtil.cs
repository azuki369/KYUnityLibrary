using UnityEngine;

namespace KyLibrary
{
    public class DebugUtil
    {


        /***********************************************************************************
            Log
        ***********************************************************************************/
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void Log(string message, UnityEngine.Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }

        public static void LogFormat(string format,params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }

        /***********************************************************************************
            Warning
        ***********************************************************************************/
        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void LogWarning(string message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogWarningFormat(format, args);
        }

        /***********************************************************************************
            Error
        ***********************************************************************************/

        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        public static void LogError(string message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(format, args);
        }


    }
}
