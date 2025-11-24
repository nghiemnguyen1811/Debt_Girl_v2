using UnityEngine;
using System.Collections.Generic;

namespace OneClickLocalization.Editor.Utils
{
    static class LanguageUtils
    {
        // bijective dictionary of systemLanguages and their codes
        // Not all codes are present with an equivalent in SystemLanguage, and we don't have a code for every SystemLanguage
        // List of codes based on https://msdn.microsoft.com/en-us/library/hh456380.aspx
        public static Dictionary<SystemLanguage, string> languagesCodes = new Dictionary<SystemLanguage, string>();

        static LanguageUtils()
        {
            languagesCodes[SystemLanguage.Afrikaans] = "af";
            languagesCodes[SystemLanguage.Arabic] = "ar";
            languagesCodes[SystemLanguage.Bulgarian] = "bg";
            languagesCodes[SystemLanguage.Catalan] = "ca";
            languagesCodes[SystemLanguage.ChineseSimplified] = "zh-CHS";
            languagesCodes[SystemLanguage.ChineseTraditional] = "zh-CHT";
            // Not sure if croatian == serboCroatian, sorry if it's your language
            languagesCodes[SystemLanguage.SerboCroatian] = "hr";
            languagesCodes[SystemLanguage.Czech] = "cs";
            languagesCodes[SystemLanguage.Danish] = "da";
            languagesCodes[SystemLanguage.Dutch] = "nl";
            languagesCodes[SystemLanguage.English] = "en";
            languagesCodes[SystemLanguage.Estonian] = "et";
            languagesCodes[SystemLanguage.Finnish] = "fi";
            languagesCodes[SystemLanguage.French] = "fr";
            languagesCodes[SystemLanguage.German] = "de";
            languagesCodes[SystemLanguage.Greek] = "el";
            languagesCodes[SystemLanguage.Hebrew] = "he";
            languagesCodes[SystemLanguage.Hungarian] = "hu";
            languagesCodes[SystemLanguage.Indonesian] = "id";
            languagesCodes[SystemLanguage.Italian] = "it";
            languagesCodes[SystemLanguage.Japanese] = "ja";
            languagesCodes[SystemLanguage.Korean] = "ko";
            languagesCodes[SystemLanguage.Latvian] = "lv";
            languagesCodes[SystemLanguage.Lithuanian] = "lt";
            languagesCodes[SystemLanguage.Norwegian] = "no";
            languagesCodes[SystemLanguage.Polish] = "pl";
            languagesCodes[SystemLanguage.Portuguese] = "pt";
            languagesCodes[SystemLanguage.Romanian] = "ro";
            languagesCodes[SystemLanguage.Russian] = "ru";
            languagesCodes[SystemLanguage.Slovak] = "sk";
            languagesCodes[SystemLanguage.Slovenian] = "sl";
            languagesCodes[SystemLanguage.Spanish] = "es";
            languagesCodes[SystemLanguage.Swedish] = "sv";
            languagesCodes[SystemLanguage.Thai] = "th";
            languagesCodes[SystemLanguage.Turkish] = "tr";
            languagesCodes[SystemLanguage.Ukrainian] = "uk";
            languagesCodes[SystemLanguage.Vietnamese] = "vi";

        }

        public static SystemLanguage GetLanguageFromCode(string code)
        {
            SystemLanguage res = SystemLanguage.Unknown;

            foreach (SystemLanguage language in languagesCodes.Keys)
            {
                if (languagesCodes[language].StartsWith(code))
                {
                    res = language;
                    break;
                }
            }

            return res;
        }

        public static string GetCodeFromLanguage(SystemLanguage language)
        {
            string res = null;
            if (languagesCodes.ContainsKey(language))
                res = languagesCodes[language];
            return res;
        }

    }
}