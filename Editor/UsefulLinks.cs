using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit
{
    public class UsefulLinks : MonoBehaviour
    {
        [MenuItem("VRWorld Toolkit/Useful Links/VRCPrefabs Database", false, 40)]
        private static void VRCPrefabsLink()
        {
            Application.OpenURL("https://vrcprefabs.com/browse");
        }

        [MenuItem("VRWorld Toolkit/Useful Links/Unofficial VRChat Wiki (EN)", false, 41)]
        private static void UnofficialWikiEN()
        {
            Application.OpenURL("http://vrchat.wikidot.com/");
        }

        [MenuItem("VRWorld Toolkit/Useful Links/VRChat 技術メモ帳 (JP)", false, 42)]
        private static void UnofficialWikiJP()
        {
            Application.OpenURL("https://vrcworld.wiki.fc2.com/");
        }

        [MenuItem("VRWorld Toolkit/Useful Links/CyanEmu", false, 43)]
        private static void CyanEmu()
        {
            Application.OpenURL("https://github.com/CyanLaser/CyanEmu");
        }

        [MenuItem("VRWorld Toolkit/Useful Links/EasyQuestSwitch", false, 44)]
        private static void EasyQuestSwitch()
        {
            Application.OpenURL("https://github.com/JordoVR/EasyQuestSwitch");
        }

#if UDON
        [MenuItem("VRWorld Toolkit/Useful Links/UdonSharp", false, 45)]
        private static void UdonSharpLink()
        {
            Application.OpenURL("https://github.com/Merlin-san/UdonSharp/");
        }
#endif

#if BAKERY_INCLUDED
        [MenuItem("VRWorld Toolkit/Useful Links/Bakery Documentation", false, 46)]
        private static void BakeryDocumentationLink()
        {
            Application.OpenURL("https://geom.io/bakery/");
        }
#endif
    }
}