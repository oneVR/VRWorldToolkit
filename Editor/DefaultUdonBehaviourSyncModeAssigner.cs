#if VRC_SDK_VRCSDK3 && UDON
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace VRWorldToolkit.Editor
{
    public static class DefaultUdonBehaviourSyncModeAssigner
    {
        private static bool waitingOnInspectorUpdate;
        
        [InitializeOnLoadMethod]
        public static void Init()
        {
            ObjectFactory.componentWasAdded += OnAddComponent;
        }
        
        private static void OnAddComponent(Component component)
        {
            if (VRWorldToolkitSettings.GetOrCreateSettings().defaultUdonBehaviourSyncMode == VRWorldToolkitSettings.AssignUdonBehaviourSyncMode.Ignore)
                return;
            
            if (component is AbstractUdonBehaviour udonBehaviour)
                udonBehaviour.SyncMethod = GetUdonBehaviourSyncModeFromSettings();
            // Wait a brief moment for any additional UdonBehaviours being added outside UnityEditor.ObjectFactory (like from UdonSharp).
            else
                CheckForPostAdditionalComponents(component, component.GetComponents<AbstractUdonBehaviour>());
        }

        private static async void CheckForPostAdditionalComponents(Component component, AbstractUdonBehaviour[] previousUdonBehaviours)
        {
            try
            {
                if (!component)
                    return;

                waitingOnInspectorUpdate = true;
                EditorApplication.delayCall += DelayCalledInspector;
                
                while (waitingOnInspectorUpdate)
                    await Task.Yield();
            
                AbstractUdonBehaviour[] newUdonBehaviours = component.GetComponents<AbstractUdonBehaviour>();
            
                if (newUdonBehaviours == null)
                    return;

                if (previousUdonBehaviours == null)
                    previousUdonBehaviours = Array.Empty<AbstractUdonBehaviour>();
                
                // Check for newly added UdonBehaviours that didn't exist before.
                foreach (AbstractUdonBehaviour udonBehaviour in newUdonBehaviours)
                {
                    if (previousUdonBehaviours.Contains(udonBehaviour))
                        continue;
                    
                    if (udonBehaviour)
                        udonBehaviour.SyncMethod = GetUdonBehaviourSyncModeFromSettings();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }
        
        private static void DelayCalledInspector()
        {
            waitingOnInspectorUpdate = false;
        }

        private static Networking.SyncType GetUdonBehaviourSyncModeFromSettings()
        {
            switch (VRWorldToolkitSettings.GetOrCreateSettings().defaultUdonBehaviourSyncMode)
            {
                case VRWorldToolkitSettings.AssignUdonBehaviourSyncMode.Continuous:
                    return Networking.SyncType.Continuous;
                case VRWorldToolkitSettings.AssignUdonBehaviourSyncMode.Manual:
                    return Networking.SyncType.Manual;
                case VRWorldToolkitSettings.AssignUdonBehaviourSyncMode.None:
                    return Networking.SyncType.None;
            }

            return default;
        }
    }
}
#endif