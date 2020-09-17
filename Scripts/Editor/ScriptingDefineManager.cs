using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit
{
    public class ScriptingDefineManager : MonoBehaviour
    {
        /// <summary>
        /// Add a new scripting define symbol in project settings
        /// </summary>
        /// <param name="define">Scripting define symbol to add</param>
        public static void AddScriptingDefine(string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();

            if (defines.Contains(define)) return;

            defines.Add(define);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", defines));
        }

        /// <summary>
        /// Remove a scripting define symbol from project settings
        /// </summary>
        /// <param name="define">Scripting define symbol to remove</param>
        public static void RemoveScriptingDefine(string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();

            if (!defines.Contains(define)) return;

            defines.Remove(define);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", defines));
        }

        /// <summary>
        /// If scripting define symbol exists
        /// </summary>
        /// <param name="define">Scripting define symbol to check for</param>
        /// <returns></returns>
        public static bool ScriptingDefineExists(string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');

            return defines.Contains(define);
        }
    }
}