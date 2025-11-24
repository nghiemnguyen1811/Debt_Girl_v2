using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OneClickLocalization.Demo.Scripts
{
    public class ScriptAPIExamples : MonoBehaviour {

        public GameObject getLocLangDropdownObj;

        public GameObject setLocLangDropdownObj;
        public GameObject setLocLangInput;

        public GameObject addLangDropdownObj;
        public GameObject removeLangDropdownObj;

        public GameObject popup;

        private PopupMessage popupMessage;

        private Dropdown getLocLangDropdown;
        private Dropdown setLocLangDropdown;
        private Dropdown addLangDropdown;
        private Dropdown removeLangDropdown;

        // Use this for initialization
        void Awake () {
            popupMessage = popup.GetComponent<PopupMessage>();

            initSystemLanguagesDropdowns();

            initSupportedLanguagesDropdowns();
        }
	
        void OnEnable()
        {
            OCL.onLanguagesChanged += OnLanguagesListChanged;
        }

        private void OnLanguagesListChanged()
        {
            initSupportedLanguagesDropdowns();
        }

        private void initSystemLanguagesDropdowns()
        {
            List<string> systemLanguages = new List<string>();
            foreach (SystemLanguage language in Enum.GetValues(typeof(SystemLanguage)))
            {
                systemLanguages.Add(language.ToString());
            }

            getLocLangDropdown = getLocLangDropdownObj.GetComponent<Dropdown>();
            getLocLangDropdown.ClearOptions();
            getLocLangDropdown.AddOptions(systemLanguages);
            setLocLangDropdown = setLocLangDropdownObj.GetComponent<Dropdown>();
            setLocLangDropdown.ClearOptions();
            setLocLangDropdown.AddOptions(systemLanguages);
            addLangDropdown = addLangDropdownObj.GetComponent<Dropdown>();
            addLangDropdown.ClearOptions();
            addLangDropdown.AddOptions(systemLanguages);
        }

        private void initSupportedLanguagesDropdowns()
        {
            List<string> languagesSupportedList = new List<string>();
            foreach (SystemLanguage language in OCL.GetLanguages(false))
            {
                languagesSupportedList.Add(language.ToString());
            }
            removeLangDropdown = removeLangDropdownObj.GetComponent<Dropdown>();
            removeLangDropdown.ClearOptions();
            removeLangDropdown.AddOptions(languagesSupportedList);
        }

        private void DisplayMessage(string message)
        {
            popup.SetActive(true);
            popupMessage.message.text = message;
        }

        public void GetLocalizationString()
        {
            string localizedValue = OCL.GetLocalization("localized text");
            if (localizedValue == null)
                localizedValue = "null";
            DisplayMessage(localizedValue);
        }

        public void GetLocalizationStringForLanguage()
        {
            SystemLanguage language = (SystemLanguage) Enum.Parse(typeof(SystemLanguage), getLocLangDropdown.options[getLocLangDropdown.value].text);
            string localizedValue = OCL.GetLocalization("localized text", language);
            if (localizedValue == null)
                localizedValue = "null";
            DisplayMessage(localizedValue);
        }

        public void SetLocalizationStringForLanguage()
        {
            SystemLanguage language = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), setLocLangDropdown.options[setLocLangDropdown.value].text);

            if (OCL.GetLanguages().Contains(language))
            {
                OCL.SetLocalization("localized text", language, setLocLangInput.GetComponent<InputField>().text);
                DisplayMessage("Localization updated");
            }
            else
            {
                DisplayMessage("Selected language is not supported, use AddLanguage()");
            }

        }

        public void AddLanguage()
        {
            SystemLanguage language = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), addLangDropdown.options[addLangDropdown.value].text);
            if (OCL.GetLanguages(false).Contains(language))
            {
                DisplayMessage("Selected language already there.");
            }
            else
            {
                OCL.AddLanguage(language);
            }
        }

        public void RemoveLanguage()
        {
            if(removeLangDropdown.options.Count > 0)
            {
                SystemLanguage language = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), removeLangDropdown.options[removeLangDropdown.value].text);
                OCL.RemoveLanguage(language);
                DisplayMessage("Language removed.");
            }
        }

        public void ToggleActive()
        {
            OCL.SetActive(!OCL.IsActive());
        }
    }
}
