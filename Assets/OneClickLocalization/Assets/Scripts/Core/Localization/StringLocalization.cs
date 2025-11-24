using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace OneClickLocalization.Core.Localization
{
    /// <summary>
    /// Class storing the AudioClip translations for a language in an OrderedDictionary.
    /// </summary>
    public class StringLocalization : ScriptableObject, ILocalization
    {
        public const string NULL_SERIALIZATION_VALUE = "[NULL VALUE OCL]";

        public SystemLanguage language = SystemLanguage.Unknown;

        public OrderedDictionary localizations = new OrderedDictionary (StringComparer.OrdinalIgnoreCase);

        // Serialization list
        public List<string> _keys = new List<string>();
        public List<string> _values = new List<string>();

        /// <summary>
        /// Serialize the OrderedDictionary using typed Lists
        /// </summary>
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            string[] myKeys = new string[localizations.Count];
            string[] myValues = new string[localizations.Count];
            localizations.Keys.CopyTo(myKeys, 0);
            localizations.Values.CopyTo(myValues, 0);

            for (int i = 0; i < localizations.Count; i++)
            {
                _keys.Add(myKeys[i]);
                if (myValues[i] == null)
                    _values.Add(NULL_SERIALIZATION_VALUE);
                else
                    _values.Add(myValues[i]);
            }

        }

        public void OnAfterDeserialize()
        {
            localizations = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i != Math.Min(_keys.Count, _values.Count); i++)
            {
                // Should not append as we do not authorize null key value in Localizations, but if it happened, it would crash all the Localization's data
                if (_keys[i] != null)
                {
                    if (_values[i].Equals(NULL_SERIALIZATION_VALUE))
                    {
                        localizations.Add(_keys[i], null);
                    }
                    else
                    {
                        localizations.Add(_keys[i], _values[i]);
                    }
                }

            }
        }

        public Type GetLocalizationType()
        {
            return typeof(string);
        }

        public object GetLocalization(object id)
        {
            if(!localizations.Contains(id))
            {
                return null;
            }
            return localizations[(string)id];
        }

        public void SetLocalization(object id, object value)
        {
            localizations[(string)id] = (string)value;
        }

        public void RemoveLocalization(object id)
        {
            if (localizations.Contains((string)id))
            {
                localizations.Remove((string)id);
            }
        }

        public void ReplaceId(object oldId, object newId)
        {
            if(oldId != null && newId != null && localizations.Contains((string)oldId) && !localizations.Contains((string)newId))
            {
                string oldValue = (string)localizations[oldId];

                string[] myKeys = new string[localizations.Count];
                localizations.Keys.CopyTo(myKeys, 0);
                int oldIndex = 0;
                for (int index = 0; index < myKeys.Length; index++)
                {
                    if (myKeys[index].Equals(oldId))
                    {
                        oldIndex = index;
                        break;
                    }
                }

                localizations.Insert(oldIndex, newId, oldValue);
                localizations.Remove((string)oldId);
            }
        }

        public List<object> GetAllIds()
        {
            List<object> localizationKeys = new List<object>();
            foreach (string localizationKey in localizations.Keys)
            {
                localizationKeys.Add(localizationKey);
            }
            return localizationKeys;
        }

        public List<object> GetAllLocalizations()
        {
            List<object> localizationValues = new List<object>();
            foreach(string localizationValue in localizations.Values)
            {
                localizationValues.Add(localizationValue);
            }
            return localizationValues;
        }

        public SystemLanguage GetLanguage()
        {
            return language;
        }

        public object Clone()
        {
            StringLocalization res = ScriptableObject.CreateInstance<StringLocalization>();

            res.language = this.language;

            res.localizations = new OrderedDictionary(localizations.Count, StringComparer.OrdinalIgnoreCase);
            string[] myKeys = new string[localizations.Count];
            string[] myValues = new string[localizations.Count];
            localizations.Keys.CopyTo(myKeys, 0);
            localizations.Values.CopyTo(myValues, 0);
            for (int i = 0; i < localizations.Count; i++)
            {
                res.localizations.Add(myKeys[i], myValues[i]);
            }

            return res;
        }

    }
}