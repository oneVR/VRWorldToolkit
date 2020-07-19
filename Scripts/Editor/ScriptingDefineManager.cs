using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit
{
    public class ScriptingDefineManager : MonoBehaviour
    {

        public static void AddScriptingDefine(string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();

            if (defines.Contains(define)) return;

            defines.Add(define);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", defines));
        }

        public static void RemoveScriptingDefine(string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();

            if (!defines.Contains(define)) return;

            defines.Remove(define);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", defines));
        }

        public static bool ScriptingDefineExists(string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');

            return defines.Contains(define);
        }
    }
}