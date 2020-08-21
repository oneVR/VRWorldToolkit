// VRCSDKClientStub
// Created by CyanLaser
// https://github.com/CyanLaser/VRCSDKClientStub
// Add this to your project to prevent sdk components throwing null references in playmode
// https://feedback.vrchat.com/sdk-bug-reports/p/vrctrigger-emits-an-error-log-on-editor

#if VRC_SDK_VRCSDK2
using System;
using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.ClientStub
{
    public class VRCSDKClientStub
    {
        // Dummy method to get the static initializer to be called early on.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod() { }

        static VRCSDKClientStub()
        {
            VRC_Trigger.InitializeTrigger = new Action<VRC_Trigger>(InitializeTrigger);
            VRCSDK2.VRC_ObjectSync.Initialize += InitializeObjectSync;
            VRCSDK2.VRC_ObjectSync.TeleportHandler += TeleportTo;
            VRCSDK2.VRC_ObjectSync.RespawnHandler += RespawnObject;
            VRCStation.Initialize += InitializeStations;
            VRCStation.useStationDelegate = UseStation;
            VRCStation.exitStationDelegate = ExitStation;

            VRC_UiShape.GetEventCamera = GetPlayerCamera;
            VRCSDK2.VRC_PlayerMods.Initialize = InitializePlayerMods;
            VRC_Pickup.OnAwake = InitializePickup;
            VRC_Pickup.ForceDrop = ForceDrop;
            VRC_Pickup._GetCurrentPlayer = GetCurrentPlayer;
            VRC_ObjectSpawn.Initialize = InitializeSpawner;
        }

        private static void InitializeSpawner(VRC.SDKBase.VRC_ObjectSpawn obj) { }

        private static VRCPlayerApi GetCurrentPlayer(VRC.SDKBase.VRC_Pickup arg) { return null; }

        private static void ForceDrop(VRC.SDKBase.VRC_Pickup obj) { }

        private static void InitializePickup(VRC.SDKBase.VRC_Pickup obj) { }

        private static void InitializePlayerMods(VRCSDK2.VRC_PlayerMods obj) { }

        private static Camera GetPlayerCamera() { return null; }

        private static void ExitStation(VRCStation arg1, VRCPlayerApi arg2) { }

        private static void UseStation(VRCStation arg1, VRCPlayerApi arg2) { }

        private static void InitializeStations(VRCStation obj) { }

        private static void RespawnObject(VRCSDK2.VRC_ObjectSync obj) { }

        private static void TeleportTo(VRCSDK2.VRC_ObjectSync obj, Vector3 position, Quaternion rotation) { }

        private static void InitializeObjectSync(VRCSDK2.VRC_ObjectSync obj) { }

        private static void InitializeTrigger(VRC.SDKBase.VRC_Trigger obj)
        {
            obj.ExecuteTrigger = ExecuteTrigger;
        }

        private static void ExecuteTrigger(VRC.SDKBase.VRC_Trigger.TriggerEvent arg) { }
    }
}
#endif