using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public class CustomEditorManager : MonoBehaviour
    {
        [MenuItem("VRWorld Toolkit/Custom Editors/Enable", false, 3)]
        private static void EnableCustomEditors()
        {
            ScriptingDefineManager.RemoveScriptingDefine("VRWT_DISABLE_EDITORS");
        }

        [MenuItem("VRWorld Toolkit/Custom Editors/Enable", true)]
        private static bool EnableCustomEditorsValidate()
        {
#if VRWT_DISABLE_EDITORS
            return true;
#else
            return false;
#endif
        }

        [MenuItem("VRWorld Toolkit/Custom Editors/Disable", false, 4)]
        private static void DisableCustomEditors()
        {
            ScriptingDefineManager.AddScriptingDefine("VRWT_DISABLE_EDITORS");
        }

        [MenuItem("VRWorld Toolkit/Custom Editors/Disable", true)]
        private static bool DisableCustomEditorsValidate()
        {
#if !VRWT_DISABLE_EDITORS
            return true;
#else
            return false;
#endif
        }
    }
}