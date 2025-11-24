using UnityEngine;
using System;
using System.Collections.Generic;

namespace OneClickLocalization.Core.Localization
{
    /// <summary>
    /// Interface defining a class storing translations for a type and a language
    /// </summary>
    public interface ILocalization : ISerializationCallbackReceiver, ICloneable
    {
        SystemLanguage GetLanguage();

        object GetLocalization(object id);

        void SetLocalization(object id, object value);

        void RemoveLocalization(object id);

        void ReplaceId(object oldId, object newId);

        List<object> GetAllIds();

        List<object> GetAllLocalizations();

        Type GetLocalizationType();
    }
}