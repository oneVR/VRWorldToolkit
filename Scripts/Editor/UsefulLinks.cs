using UnityEditor;
using UnityEngine;

public class UsefulLinks : MonoBehaviour
{
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
