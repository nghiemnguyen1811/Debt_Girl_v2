using UnityEditor;
using UnityEngine;

namespace OneClickLocalization.Editor
{
    public class OCLMenu : MonoBehaviour
    {

        private static readonly string documentationUrl = "http://www.redgirafegames.com/doc/OCL.html";
    
        [MenuItem("Window/One Click Localization/Setup %&l")]
        public static void ShowSetupWindow()
        {
            OCLSetupWindow.ShowWindow();
        }

        [MenuItem("Window/One Click Localization/Edit Localizations #%&l")]
        public static void ShowLocalizationWindow()
        {
            OCLLocalizationWindow window = OCLLocalizationWindow.ShowWindow();
            window.minSize = new Vector2(400, 200);
        }

        [MenuItem("Window/One Click Localization/Online Documentation")]
        public static void ShowDocumentation()
        {
            Application.OpenURL(documentationUrl);
        }

        // DEBUG
        /*
    [MenuItem("Test/azureTranslation")]
    public static void azure()
    {
        AzureTranslator trans = new AzureTranslator("1fc77822b0034cacb80bbcf37daa0c50");
        string t = trans.Translate("je suis là", SystemLanguage.French, SystemLanguage.English);

        Debug.Log("translation : " + t);
    }
    */
    }
}
