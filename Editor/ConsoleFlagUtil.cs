using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace VRWorldToolkit
{
    /// <summary>
    /// Utility for setting and getting console error pause flag with reflection
    /// </summary>
    public static class ConsoleFlagUtil
    {
        private static readonly System.Type systemType;
        private static MethodInfo mMethod_GetConsoleErrorPause;
        private static MethodInfo mMethod_SetConsoleErrorPause;

        static ConsoleFlagUtil()
        {
            systemType = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.ConsoleWindow");
            Assert.IsNotNull(systemType);
        }

        public static bool GetConsoleErrorPause()
        {
            if (mMethod_GetConsoleErrorPause == null)
                mMethod_GetConsoleErrorPause = systemType.GetMethod("GetConsoleErrorPause", BindingFlags.Static | BindingFlags.Public);

            Assert.IsNotNull(mMethod_GetConsoleErrorPause);
            return (bool)mMethod_GetConsoleErrorPause.Invoke(null, null);
        }

        public static void SetConsoleErrorPause(Boolean enabled)
        {
            if (mMethod_SetConsoleErrorPause == null)
                mMethod_SetConsoleErrorPause = systemType.GetMethod("SetConsoleErrorPause", BindingFlags.Static | BindingFlags.Public);

            Assert.IsNotNull(mMethod_SetConsoleErrorPause);
            mMethod_SetConsoleErrorPause.Invoke(null, new object[] { enabled });
        }
    }
}