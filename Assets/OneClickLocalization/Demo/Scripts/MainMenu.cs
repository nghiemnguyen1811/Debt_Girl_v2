using UnityEngine;

namespace OneClickLocalization.Demo.Scripts
{
    public class MainMenu : MonoBehaviour {

        private GameObject mainMenu;
        private GameObject uiExamples;
        private GameObject textMeshProExamples;
        private GameObject audioExamples;
        private GameObject textMeshExamples;
        private GameObject scripAPIExamples;
        private GameObject bottomOptions;

        // Use this for initialization
        void Awake ()
        {
            mainMenu = GameObject.Find("MainMenu");
            uiExamples = GameObject.Find("UIExamples");
            uiExamples.SetActive(false);
            textMeshProExamples = GameObject.Find("TextMeshProExamples");
            textMeshProExamples.SetActive(false);
            audioExamples = GameObject.Find("AudioExamples");
            audioExamples.SetActive(false);
            textMeshExamples = GameObject.Find("TextMeshExamples");
            textMeshExamples.SetActive(false);
            scripAPIExamples = GameObject.Find("ScriptAPIExamples");
            scripAPIExamples.SetActive(false);
            bottomOptions = GameObject.Find("BottomOptions");
            bottomOptions.SetActive(false);
        }
	
        // Update is called once per frame
        void Update () {
	
        }

        public void ShowUIExamples()
        {
            mainMenu.SetActive(false);
            uiExamples.SetActive(true);
            bottomOptions.SetActive(true);
        }

        public void ShowTextMeshProExamples()
        {
            mainMenu.SetActive(false);
            textMeshProExamples.SetActive(true);
            bottomOptions.SetActive(true);
        }

        public void ShowAudioExamples()
        {
            mainMenu.SetActive(false);
            audioExamples.SetActive(true);
            bottomOptions.SetActive(true);
        }
     
        public void ShowTextMeshExamples()
        {
            mainMenu.SetActive(false);
            textMeshExamples.SetActive(true);
            bottomOptions.SetActive(true);
        }

        public void ShowScriptAPIExamples()
        {
            mainMenu.SetActive(false);
            scripAPIExamples.SetActive(true);
            bottomOptions.SetActive(true);
        }

        public void BackToMenu()
        {
            mainMenu.SetActive(true);
            uiExamples.SetActive(false);
            textMeshProExamples.SetActive(false);
            audioExamples.SetActive(false);
            textMeshExamples.SetActive(false);
            scripAPIExamples.SetActive(false);
            bottomOptions.SetActive(false);
        }
    }
}
