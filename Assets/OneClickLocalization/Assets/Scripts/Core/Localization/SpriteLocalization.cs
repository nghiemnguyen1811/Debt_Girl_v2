using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace OneClickLocalization.Core.Localization
{
    /// <summary>
    /// Class storing the Sprite translations for a language in an OrderedDictionary.
    /// </summary>
    public class SpriteLocalization : ScriptableObject, ILocalization
    {
        public SystemLanguage language = SystemLanguage.Unknown;

        public OrderedDictionary localizations = new OrderedDictionary();

        // Serialization list
        public List<Sprite> _keys = new List<Sprite>();
        public List<Sprite> _values = new List<Sprite>();

        /// <summary>
        /// Serialize the OrderedDictionary using typed Lists
        /// </summary>
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            Sprite[] myKeys = new Sprite[localizations.Count];
            Sprite[] myValues = new Sprite[localizations.Count];
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
                // Should not append as we do not authorize null key value in Localizations, but if it happened, it would crash all the Localization's data
                if (_keys[i] != null)
                {
                    localizations.Add(_keys[i], _values[i]);
                }
            }
        }

        public Type GetLocalizationType()
        {
            return typeof(Sprite);
        }

        public object GetLocalization(object id)
        {
            if (!localizations.Contains((Sprite)id))
            {
                return null;
            }
            return localizations[(Sprite)id];
        }

        public void SetLocalization(object id, object value)
        {
            localizations[(Sprite)id] = (Sprite)value;
        }

        public void RemoveLocalization(object id)
        {
            if (localizations.Contains((Sprite)id))
            {
                localizations.Remove((Sprite)id);
            }
        }

        public void ReplaceId(object oldId, object newId)
        {
            if (oldId != null && newId != null && localizations.Contains((Sprite)oldId) && !localizations.Contains((Sprite)newId))
            {
                Sprite oldValue = (Sprite)localizations[oldId];

                Sprite[] myKeys = new Sprite[localizations.Count];
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
                localizations.Remove((Sprite)oldId);
            }
        }

        public List<object> GetAllIds()
        {
            List<object> localizationKeys = new List<object>();
            foreach (Sprite localizationKey in localizations.Keys)
            {
                localizationKeys.Add(localizationKey);
            }
            return localizationKeys;
        }

        public List<object> GetAllLocalizations()
        {
            List<object> localizationValues = new List<object>();
            foreach(Sprite localizationValue in localizations.Values)
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
            SpriteLocalization res = SpriteLocalization.CreateInstance<SpriteLocalization>();

            res.language = this.language;

            res.localizations = new OrderedDictionary(localizations.Count);
            Sprite[] myKeys = new Sprite[localizations.Count];
            Sprite[] myValues = new Sprite[localizations.Count];
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