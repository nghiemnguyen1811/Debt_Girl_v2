using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OneClickLocalization.Demo.Scripts
{
    public class GlobalOptions : MonoBehaviour {

        public Toggle activeToggle;
        public Toggle languageAutoToggle;
        public Dropdown languageDropdown;

        // Use this for initialization
        void Start () {
            // Active checkbox
            activeToggle.isOn = OCL.IsActive();

            // Auto language checkbox
            bool isAuto = false;
            OCL.setLanguageAuto(isAuto);
            languageAutoToggle.isOn = isAuto;

            // language Dropdown
            initLanguageDropdown();
        }

        void OnEnable()
        {
            OCL.onLanguageChanged += OnLanguageChanged;
            OCL.onActiveChanged += OnActiveChanged;
            OCL.onLanguagesChanged += OnLanguagesChanged;
        }

        public void ActiveChangedHandler()
        {
            OCL.SetActive(activeToggle.isOn);
        }

        /// <summary>
        /// Modification of the active state made by an OCL API call
        /// </summary>
        /// <param name="activeState"></param>
        public void OnActiveChanged(bool activeState)
        {
            activeToggle.isOn = OCL.IsActive();
        }

        /// <summary>
        /// Modification of the supported languages made by an OCL API call
        /// </summary>
        public void OnLanguagesChanged()
        {
            initLanguageDropdown();
        }

        public void LanguageAutoChangedHandler()
        {
            bool isAuto = languageAutoToggle.isOn;
            OCL.setLanguageAuto(isAuto);
            languageDropdown.interactable = !isAuto;
        }

        /// <summary>
        /// Modification of the language by the dropdown
        /// </summary>
        public void LanguageChangedHandler()
        {
            string selectedLanguage = languageDropdown.options[languageDropdown.value].text;
            Debug.Log("language changed : " + selectedLanguage);
            OCL.SetLanguage((SystemLanguage) Enum.Parse(typeof(SystemLanguage), selectedLanguage));
        }

        /// <summary>
        /// Modification of the language made by an OCL API call
        /// </summary>
        /// <param name="oldLang"></param>
        /// <param name="newLang"></param>
        private void OnLanguageChanged(SystemLanguage oldLang, SystemLanguage newLang)
        {
            int newIndex = -1;
            foreach (Dropdown.OptionData option in languageDropdown.options)
            {
                if (option.text.Equals(newLang.ToString()))
                {
                    newIndex = languageDropdown.options.IndexOf(option);
                }
            }

            if (newIndex != languageDropdown.value)
            {
                languageDropdown.value = newIndex;
            }
        }

        private void initLanguageDropdown()
        {
            languageDropdown.interactable = !OCL.IsLanguageAuto();
            languageDropdown.options.Clear();

            // Languages list init
            List<string> languagesStrings = new List<string>();
            // Add supported languages
            foreach (SystemLanguage supportedLanguage in OCL.GetLanguages())
            {
                languagesStrings.Add(supportedLanguage.ToString());
            }
            languageDropdown.AddOptions(languagesStrings);

            // Init dropdown with current OCL language
            SystemLanguage currentLanguage = OCL.GetLanguage();
            foreach (Dropdown.OptionData option in languageDropdown.options)
            {
                if (option.text.Equals(currentLanguage.ToString()))
                {
                    languageDropdown.value = languageDropdown.options.IndexOf(option);
                    break;
                }
            }
        }
    }
}
