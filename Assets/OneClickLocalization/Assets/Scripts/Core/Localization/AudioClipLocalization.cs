using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace OneClickLocalization.Core.Localization
{
    /// <summary>
    /// Class storing the AudioClip translations for a language in an OrderedDictionary.
    /// </summary>
    public class AudioClipLocalization : ScriptableObject, ILocalization
    {
        public SystemLanguage language = SystemLanguage.Unknown;

        public OrderedDictionary localizations = new OrderedDictionary();

        // Serialization list
        public List<AudioClip> _keys = new List<AudioClip>();
        public List<AudioClip> _values = new List<AudioClip>();

        /// <summary>
        /// Serialize the OrderedDictionary using typed Lists
        /// </summary>
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            AudioClip[] myKeys = new AudioClip[localizations.Count];
            AudioClip[] myValues = new AudioClip[localizations.Count];
            localizations.Keys.CopyTo(myKeys, 0);
            localizations.Values.CopyTo(myValues, 0);

            for (int i = 0; i < localizations.Count; i++)
            {
                _keys.Add(myKeys[i]);
                _values.Add(myValues[i]);
            }
        }
        public void OnAfterDeserialize()
        {
            localizations = new OrderedDictionary();

            for (var i = 0; i != Math.Min(_keys.Count, _values.Count); i++)
            {
                // Should not append as we do not authorize null key value in Localizations, but if it happend, it would crash all the Localization's data
                if (_keys[i] != null)
                {
                    localizations.Add(_keys[i], _values[i]);
                }
            }
        }

        public Type GetLocalizationType()
        {
            return typeof(AudioClip);
        }

        public object GetLocalization(object id)
        {
            if (!localizations.Contains((AudioClip)id))
            {
                return null;
            }
            return localizations[(AudioClip)id];
        }

        public void SetLocalization(object id, object value)
        {
            localizations[(AudioClip)id] = (AudioClip)value;
        }

        public void RemoveLocalization(object id)
        {
            if (localizations.Contains((AudioClip)id))
            {
                localizations.Remove((AudioClip)id);
            }
        }

        public void ReplaceId(object oldId, object newId)
        {
            if (oldId != null && newId != null && localizations.Contains((AudioClip)oldId) && !localizations.Contains((AudioClip)newId))
            {
                AudioClip oldValue = (AudioClip)localizations[oldId];

                AudioClip[] myKeys = new AudioClip[localizations.Count];
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
                localizations.Remove((AudioClip)oldId);
            }
        }

        public List<object> GetAllIds()
        {
            List<object> ids = new List<object>();
            foreach (AudioClip id in localizations.Keys)
            {
                ids.Add(id);
            }
            return ids;
        }

        public List<object> GetAllLocalizations()
        {
            List<object> localizationValues = new List<object>();
            foreach(AudioClip localizationValue in localizations.Values)
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
            AudioClipLocalization res = ScriptableObject.CreateInstance<AudioClipLocalization>();

            res.language = this.language;

            res.localizations = new OrderedDictionary(localizations.Count);
            AudioClip[] myKeys = new AudioClip[localizations.Count];
            AudioClip[] myValues = new AudioClip[localizations.Count];
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