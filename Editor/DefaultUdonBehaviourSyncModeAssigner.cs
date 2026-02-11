#if VRC_SDK_VRCSDK3 && UDON
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace VRWorldToolkit.Editor
{
    public static class DefaultUdonBehaviourSyncModeAssigner
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
            ObjectFactory.componentWasAdded += OnAddComponent;
        }
        
        private static void OnAddComponent(Component component)
        {
            if (component is AbstractUdonBehaviour udonBehaviour)
                udonBehaviour.SyncMethod = Networking.SyncType.None;
        }
    }
}
#endif