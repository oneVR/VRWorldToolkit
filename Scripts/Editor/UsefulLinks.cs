using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit
{
    public class UsefulLinks : MonoBehaviour
    {
#if VRC_SDK_VRCSDK2 || !VRC_SDK_VRCSDK3
        [MenuItem("VRWorld Toolkit/Useful Links/Download SDK2", false, 100)]
        private static void SDK2Download()
        {
            Application.OpenURL("https://vrchat.com/download/sdk2");
        }
#endif

#if VRC_SDK_VRCSDK3 || !VRC_SDK_VRCSDK2
        [MenuItem("VRWorld Toolkit/Useful Links/Download SDK3", false, 100)]
        private static void SDK3Download()
        {
            Application.OpenURL("https://vrchat.com/download/sdk3-worlds");
        }
#endif

        [MenuItem("VRWorld Toolkit/Useful Links/VRCPrefabs Database", false, 200)]
        private static void VRCPrefabsLink()
        {
            Application.OpenURL("https://vrcprefabs.com/browse");
        }

        [MenuItem("VRWorld Toolkit/Useful Links/Unofficial VRChat Wiki (EN)", false, 200)]
        private static void UnofficialWikiEN()
        {
            Application.OpenURL("http://vrchat.wikidot.com/");
        }

        [MenuItem("VRWorld Toolkit/Useful Links/VRChat 技術メモ帳 (JP)", false, 200)]
        private static void UnofficialWikiJP()
        {
            Application.OpenURL("https://vrcworld.wiki.fc2.com/");
        }

#if UDON
        [MenuItem("VRWorld Toolkit/Useful Links/UdonSharp", false, 200)]
        private static void UdonSharpLink()
        {
            Application.OpenURL("https://github.com/Merlin-san/UdonSharp/releases");
        }
#endif

#if BAKERY_INCLUDED
        [MenuItem("VRWorld Toolkit/Useful Links/Bakery Documentation", false, 200)]
        private static void BakeryDocumentationLink()
        {
            Application.OpenURL("https://geom.io/bakery/");
        }
#endif
    }
}
