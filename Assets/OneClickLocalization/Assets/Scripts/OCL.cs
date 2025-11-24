using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using OneClickLocalization.Core;

namespace OneClickLocalization
{
    /// <summary>
    /// One Click Localization Manager.
    /// Use this class to access localizations and to modify its setup.
    /// 
    /// </summary>
    public class OCL
    {
        // delegates
        public delegate void ActiveChanged(bool isActive);
        public static ActiveChanged onActiveChanged;
        public delegate void LanguageChanged(SystemLanguage oldLang, SystemLanguage newLang);
        public static LanguageChanged onLanguageChanged;
        public delegate void LanguagesChanged();
        public static LanguagesChanged onLanguagesChanged;
        public delegate void LocalizationChanged(object id, SystemLanguage language, object newValue);
        public static LocalizationChanged onLocalizationChanged;

        // OCL setup, loaded and cloned from disk on start and modified by the API during runtime
        protected static LocalizationSetup runtimeSetup;

        // Pattern used to find dynamic parameters in strings
        protected static string parameterPattern = @"\${([0-9])*\}";
        protected static List<string> stringsWithParameters;
        protected static List<Regex> stringsWithParametersRegex;
        
        /// <summary>
        /// Static constructor
        /// Initialize setup
        /// </summary>
        static OCL()
        {
            // initialize data from persistent asset
            LocalizationSetup persistedSetup = Resources.Load<LocalizationSetup>(LocalizationSetup.setupResourceName);
            if (persistedSetup == null)
            {
                Debug.LogWarning("[OCL] No setup asset <" + LocalizationSetup.setupResourceName + "> found on disk. Initializing deactivated empty setup.");

                runtimeSetup = ScriptableObject.CreateInstance<LocalizationSetup>();
                runtimeSetup.active = false;
                runtimeSetup.AddLanguage(runtimeSetup.defaultLanguage);
            }
            else
            {
                // Isolate the runtime setup from the editor, usefull for in editor tests
                runtimeSetup = (LocalizationSetup) persistedSetup.Clone();
                runtimeSetup.LoadSubAssets(true);
            }
            initStringsWithParameters();
        }
        
        /// <summary>
        /// Initialize the cache for strings with parameters to make lookup faster
        /// </summary>
        protected static void initStringsWithParameters()
        {
            stringsWithParameters = new List<string>();
            stringsWithParametersRegex = new List<Regex>();

            foreach(string stringId in runtimeSetup.GetIds<string>())
            {
                string[] substrings = Regex.Split(stringId, parameterPattern);
                if (substrings.Length > 1)
                {

                    int index = 0;
                    string res = "";
                    foreach (string match in substrings)
                    {
                        if (index % 2 == 0)
                        {
                            res += match;
                        }
                        else
                        {
                            res += "(.*)";
                        }

                        index++;
                    }
                    Regex stringRegex = new Regex(res, RegexOptions.IgnoreCase);
                    stringsWithParameters.Add(stringId);
                    stringsWithParametersRegex.Add(stringRegex);
                }
            }
        }

        /// <summary>
        /// Returns if Localization is active.
        /// </summary>
        /// <returns></returns>
        public static bool IsActive()
        {
            return runtimeSetup.active;
        }
        /// <summary>
        /// Set Localization status.
        /// </summary>
        /// <param name="value"></param>
        public static void SetActive(bool value)
        {
            runtimeSetup.active = value;
            if(onActiveChanged != null)
            {
                onActiveChanged(IsActive());
            }
        }

        /// <summary>
        /// Set the languages used by OCL, has not effect if IsLanguageAuto is true.
        /// </summary>
        /// <param name="language"></param>
        public static void SetLanguage(SystemLanguage language)
        {
            if (runtimeSetup.forcedLanguage != language)
            {
                SystemLanguage oldLanguage = runtimeSetup.forcedLanguage;
                runtimeSetup.forcedLanguage = language;

                // Fire change event
                if(onLanguageChanged != null)
                {
                    onLanguageChanged(oldLanguage, language);
                }
            }
        }
        /// <summary>
        /// Returns language used by OCL.
        /// If IsLanguageAuto is true : returns Application.systemLanguage.
        /// If IsLanguageAuto is false : returns language defined with SetLanguage.
        /// 
        /// Default value is SystemLanguage.English
        /// </summary>
        /// <returns></returns>
        public static SystemLanguage GetLanguage()
        {
            if (IsLanguageAuto())
            {
                return Application.systemLanguage;
            }
            else
            {
                return runtimeSetup.forcedLanguage;
            }
        }

        /// <summary>
        /// If true, OCL uses Application.systemLanguage for localization.
        /// If false, OCL uses GetCustomLanguage for localization
        /// </summary>
        /// <returns></returns>
        public static bool IsLanguageAuto()
        {
            return !runtimeSetup.forceLanguage;
        }     
        /// <summary>
        /// Defines if OCL should use Application.systemLanguage (value true) or GetLanguage (value false) for localization.
        /// </summary>
        /// <param name="isAuto"></param>
        public static void setLanguageAuto(bool isAuto)
        {
            if (isAuto == runtimeSetup.forceLanguage)
            {
                runtimeSetup.forceLanguage = !isAuto;

                if (Application.systemLanguage != runtimeSetup.forcedLanguage)
                {
                    if (isAuto)
                    {
                        if(onLanguageChanged != null)
                        {
                            onLanguageChanged(runtimeSetup.forcedLanguage, Application.systemLanguage);
                        }

                    }
                    else
                    {
                        if (onLanguageChanged != null)
                        {
                            onLanguageChanged(Application.systemLanguage, runtimeSetup.forcedLanguage);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generic version of GetLocalization.
        /// Returns localization for id and current language.
        /// 
        /// Supported types are : 
        ///     - string
        ///     - Texture
        ///     - Sprite
        ///     - AudioClip
        ///     
        /// Returns null if id has no localization for current language or id's type is not supported.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Translated version of id, null if no localization found or type of id not supported</returns>
        static public object GetLocalization(object id)
        {
            if(id is string)
            {
                return GetLocalization((string)id);
            }
            else if(id is Sprite)
            {
                return GetLocalization((Sprite)id);
            }
            else if (id is Texture)
            {
                return GetLocalization((Texture)id);
            }
            else if(id is AudioClip)
            {
                return GetLocalization((AudioClip)id);
            }
            else
            {
                Debug.LogWarning("GetLocalization could not translate type : " + id.GetType());
                return null;
            }
        }
        /// <summary>
        /// Returns localization of stringId for current language.
        /// </summary>
        /// <param name="stringId"></param>
        /// <returns>Translated version of originalString, null if no localization found, originalString if language is defaultLanguage or useDefaultLanguageForNullValues is true and translation is null</returns>
        static public string GetLocalization(string stringId)
        {
            string res = null;
            
            if (!runtimeSetup.active)
            {
                return res;
            }

            SystemLanguage language = Application.systemLanguage;
            if (runtimeSetup.forceLanguage)
            {
                language = runtimeSetup.forcedLanguage;
            }

            // return original value for defaultLanguage, only if it is not overriden in config
            if (language.Equals(runtimeSetup.defaultLanguage) && !runtimeSetup.languages.Contains(runtimeSetup.defaultLanguage))
            {
                return stringId;
            }

            res = GetLocalization(stringId, language);

            // If there is no translation for this language, use the default language value if configured
            if (res == null && runtimeSetup.useDefaultLanguageForNullValues)
                return stringId;

            return res;
        }
        /// <summary>
        /// Returns localization of spriteId for current language.
        /// </summary>
        /// <param name="spriteId"></param>
        /// <returns>Translated version of originalSprite, null if no localization found or useDefaultLanguageForNullValues is true and translation is null</returns>
        static public Sprite GetLocalization(Sprite spriteId)
        {
            Sprite res = null;

            if (!runtimeSetup.active)
            {
                return res;
            }

            SystemLanguage language = Application.systemLanguage;
            if (runtimeSetup.forceLanguage)
            {
                language = runtimeSetup.forcedLanguage;
            }

            // return original value for defaultLanguage, only if it is not overriden in config
            if (language.Equals(runtimeSetup.defaultLanguage) && !runtimeSetup.languages.Contains(runtimeSetup.defaultLanguage))
            {
                return spriteId;
            }

            res = GetLocalization(spriteId, language);

            // If there is no translation for this language, use the default language value if configured
            if (res == null && runtimeSetup.useDefaultLanguageForNullValues)
                return spriteId;

            return res;
        }
        /// <summary>
        /// Returns localization of textureId for current language.
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns>Translated version of originalTexture, null if no localization found or useDefaultLanguageForNullValues is true and translation is null</returns>
        static public Texture GetLocalization(Texture textureId)
        {
            Texture res = null;

            if (!runtimeSetup.active)
            {
                return res;
            }

            SystemLanguage language = Application.systemLanguage;
            if (runtimeSetup.forceLanguage)
            {
                language = runtimeSetup.forcedLanguage;
            }

            // return original value for defaultLanguage, only if it is not overriden in config
            if (language.Equals(runtimeSetup.defaultLanguage) && !runtimeSetup.languages.Contains(runtimeSetup.defaultLanguage))
            {
                return textureId;
            }

            res = GetLocalization(textureId, language);

            // If there is no translation for this language, use the default language value if configured
            if (res == null && runtimeSetup.useDefaultLanguageForNullValues)
                return textureId;

            return res;
        }
        /// <summary>
        /// Returns localization of audioClipId for current language.
        /// </summary>
        /// <param name="audioClipId"></param>
        /// <returns>Translated version of originalAudioClip, null if no localization found or useDefaultLanguageForNullValues is true and translation is null</returns>
        static public AudioClip GetLocalization(AudioClip audioClipId)
        {
            AudioClip res = null;

            if (!runtimeSetup.active)
            {
                return res;
            }

            SystemLanguage language = Application.systemLanguage;
            if (runtimeSetup.forceLanguage)
            {
                language = runtimeSetup.forcedLanguage;
            }

            // return original value for defaultLanguage, only if it is not overriden in config
            if (language.Equals(runtimeSetup.defaultLanguage) && !runtimeSetup.languages.Contains(runtimeSetup.defaultLanguage))
            {
                return audioClipId;
            }

            res = GetLocalization(audioClipId, language);

            // If there is no translation for this language, use the default language value if configured
            if (res == null && runtimeSetup.useDefaultLanguageForNullValues)
                return audioClipId;

            return res;
        }
        /// <summary>
        /// Returns localization of stringId for language.
        /// This method shouldn't be called directly as it won't handle active, defaultLanguage, useDefaultLanguageForNullValues and forceLanguage parameters.
        /// Call it only if you need to access localization data directly without taking care of OCL setup.
        /// </summary>
        /// <param name="stringId"></param>
        /// <param name="language"></param>
        /// <returns>null if no localization found</returns>
        static public string GetLocalization(string stringId, SystemLanguage language)
        {
            string res = (string)runtimeSetup.GetLocalization(stringId, language);
            
            // Simple translation not found, try regex
            if(res == null)
            {
                // regex string lookup
                foreach (Regex reg in stringsWithParametersRegex)
                {
                    if (reg.IsMatch(stringId))
                    {
                        List<String> parametersValues = new List<string>();
                        GroupCollection groups = reg.Match(stringId).Groups;
                        for (int i=1; i<groups.Count; i++)
                        {
                            Group g = groups[i];
                            parametersValues.Add(g.ToString());
                        }

                        int index = stringsWithParametersRegex.IndexOf(reg);

                        string originalWithParameters = stringsWithParameters[index];

                        List<string> parameters = new List<string>();
                        string[] substrings = Regex.Split(originalWithParameters, parameterPattern);
                        for (int i = 0; i < substrings.Length; i++)
                        {
                            if(i % 2 != 0)
                            {
                                parameters.Add(substrings[i]);
                            }
                        }

                        string realKey = (string)runtimeSetup.GetLocalization(originalWithParameters, language);
                        if(realKey != null) {

                            string finalString = "";
                            string[] realKeyElements = Regex.Split(realKey, parameterPattern);
                            for (int i = 0; i < realKeyElements.Length; i++)
                            {
                                if (i % 2 == 0)
                                {
                                    finalString += realKeyElements[i];
                                }
                                else
                                {
                                    if(parameters.IndexOf(realKeyElements[i]) != -1)
                                    {
                                        finalString += parametersValues[parameters.IndexOf(realKeyElements[i])];
                                    }
                                    else
                                    {
                                        finalString += realKeyElements[i];
                                    }
                                }
                            }

                            res = finalString;
                        }

                    }
                }
            }

            return res;
        }
        /// <summary>
        /// Returns localization of spriteId for language.
        /// This method shouldn't be called directly as it won't handle active, defaultLanguage, useDefaultLanguageForNullValues and forceLanguage parameters.
        /// Call it only if you need to access localization data directly without taking care of OCL setup.
        /// </summary>
        /// <param name="originalString"></param>
        /// <param name="language"></param>
        /// <returns>null if no localization found</returns>
        static public Sprite GetLocalization(Sprite spriteId, SystemLanguage language)
        {
            Sprite res = (Sprite)runtimeSetup.GetLocalization(spriteId, language);
            
            return res;
        }
        /// <summary>
        /// Returns localization of textureId for language.
        /// This method shouldn't be called directly as it won't handle active, defaultLanguage  and forceLanguage parameters.
        /// Call it only if you need to access localization data directly without taking care of OCL setup.
        /// </summary>
        /// <param name="originalString"></param>
        /// <param name="language"></param>
        /// <returns>null if no localization found</returns>
        static public Texture GetLocalization(Texture textureId, SystemLanguage language)
        {
            Texture res = (Texture)runtimeSetup.GetLocalization(textureId, language);

            return res;
        }
        /// <summary>
        /// Returns localization of audioClipId for language.
        /// This method shouldn't be called directly as it won't handle active, defaultLanguage, useDefaultLanguageForNullValues and forceLanguage parameters.
        /// Call it only if you need to access localization data directly without taking care of OCL setup.
        /// </summary>
        /// <param name="originalString"></param>
        /// <param name="language"></param>
        /// <returns>null if no localization found</returns>
        static public AudioClip GetLocalization(AudioClip audioClipId, SystemLanguage language)
        {
            AudioClip res = (AudioClip)runtimeSetup.GetLocalization(audioClipId, language);

            return res;
        }

        /// <summary>
        /// Generic version of SetLocalization.
        /// Set the translation for id and language.
        /// Has no effect if language is not in GetLanguages. Use AddLanguage to add a new language.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <param name="translation"></param>
        static public void SetLocalization(object id, SystemLanguage language, object translation)
        {
            runtimeSetup.SetLocalization(id, language, translation);
            if (onLocalizationChanged != null)
            {
                onLocalizationChanged(id, language, translation);
            }
        }
        /// <summary>
        /// Set the string translation for id and language.
        /// Has no effect if language is not in GetLanguages. Use AddLanguage to add a new language.
        /// </summary>
        /// <param name="stringId"></param>
        /// <param name="language"></param>
        /// <param name="translation"></param>
        static public void SetLocalization(string stringId, SystemLanguage language, string translation)
        {
            runtimeSetup.SetLocalization(stringId, language, translation);
            if (onLocalizationChanged != null)
            {
                onLocalizationChanged(stringId, language, translation);
            }
        }
        /// <summary>
        /// Set the Sprite translation id and language
        /// Has no effect if language is not in GetLanguages. Use AddLanguage to add a new language.
        /// </summary>
        /// <param name="spriteId"></param>
        /// <param name="language"></param>
        /// <param name="translation"></param>
        static public void SetLocalization(Sprite spriteId, SystemLanguage language, Sprite translation)
        {
            runtimeSetup.SetLocalization(spriteId, language, translation);
            if (onLocalizationChanged != null)
            {
                onLocalizationChanged(spriteId, language, translation);
            }
        }
        /// <summary>
        /// Set the Texture translation for id and language
        /// Has no effect if language is not in GetLanguages. Use AddLanguage to add a new language.
        /// </summary>
        /// <param name="textureId"></param>
        /// <param name="language"></param>
        /// <param name="translation"></param>
        static public void SetLocalization(Texture textureId, SystemLanguage language, Texture translation)
        {
            runtimeSetup.SetLocalization(textureId, language, translation);
            if (onLocalizationChanged != null)
            {
                onLocalizationChanged(textureId, language, translation);
            }
        }
        /// <summary>
        /// Set the AudioClip translation for id and language
        /// Has no effect if language is not in GetLanguages. Use AddLanguage to add a new language.
        /// </summary>
        /// <param name="audioClipId"></param>
        /// <param name="language"></param>
        /// <param name="translation"></param>
        static public void SetLocalization(AudioClip audioClipId, SystemLanguage language, AudioClip translation)
        {
            runtimeSetup.SetLocalization(audioClipId, language, translation);
            if (onLocalizationChanged != null)
            {
                onLocalizationChanged(audioClipId, language, translation);
            }
        }

        /// <summary>
        /// Add a new language.
        /// Has no effect if language is already in GetLanguages
        /// </summary>
        /// <param name="language"></param>
        static public void AddLanguage(SystemLanguage language)
        {
            runtimeSetup.AddLanguage(language);
            if(onLanguagesChanged != null)
            {
                onLanguagesChanged();
            }
        }
        /// <summary>
        /// Removes a language.
        /// Has no effect if language is not in GetLanguages.
        /// By removing a language, you lose all translations attached to it.
        /// </summary>
        /// <param name="language"></param>
        static public void RemoveLanguage(SystemLanguage language)
        {
            runtimeSetup.RemoveLanguage(language);
            if (onLanguagesChanged != null)
            {
                onLanguagesChanged();
            }
        }

        /// <summary>
        /// Returns languages.
        /// Use AddLanguage to add a new one and RemoveLanguage to remove one.
        /// 
        /// If addDefaultLanguage is false, defaultLanguage won't be returned in the list.
        /// </summary>
        /// <returns></returns>
        static public List<SystemLanguage> GetLanguages(bool addDefaultLanguage = true)
        {
            List<SystemLanguage> res = new List<SystemLanguage>(runtimeSetup.languages);
            // virtually add defaultLanguage to the list 
            if (addDefaultLanguage)
            {
                res.Insert(0, runtimeSetup.defaultLanguage);
            }
            return res;
        }

    }
}