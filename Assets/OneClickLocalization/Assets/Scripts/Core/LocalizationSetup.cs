using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using OneClickLocalization.Core.Localization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OneClickLocalization.Core
{
    /// <summary>
    /// Class storing all the setup.
    /// Translations are stored in ILocalization.
    /// 
    /// LocalizationSetup is persisted on disk as an asset in Resources folder.
    /// All the ILocalization, storing the translations, are persisted as sub-assets of LocalizationSetup.
    /// 
    /// At runtime, OCL class is cloning the current setup to use it as its dataSource.
    /// 
    /// </summary>
    [Serializable]
    public class LocalizationSetup : ScriptableObject, ICloneable
    {
        // Path to store setup on disk
        public static string setupPersistPath = "Assets/OneClickLocalization/Resources";
        public static string setupPersistName = "OCLSetup.asset";
        // Path to get setup from Resources at runtime
        public static string setupResourceName = setupPersistName.Substring(0, setupPersistName.IndexOf("."));
        
        public List<ILocalization> allLocalizations = new List<ILocalization>();

        public SystemLanguage defaultLanguage = SystemLanguage.English;

        public List<SystemLanguage> languages = new List<SystemLanguage>();

        public bool active = true;

        public bool forceLanguage = false;
        public SystemLanguage forcedLanguage = SystemLanguage.English;

        public bool useDefaultLanguageForNullValues = false;

        public LocalizationSetup(){}
        
        /// <summary>
        /// Load setup subAssets
        /// Runtime uses cloneInstances=true to isolate from editor
        /// </summary>
        public void LoadSubAssets(bool cloneInstances)
        {
            allLocalizations = new List<ILocalization>();

            UnityEngine.Object[] assets = Resources.LoadAll(setupResourceName);

            bool defaultFound = false;
            foreach (UnityEngine.Object asset in assets)
            {
                if(asset != this && asset is ILocalization)
                {
                    if (((ILocalization)asset).GetLanguage().Equals(defaultLanguage))
                    {
                        defaultFound = true;
                    }
                    // add new translation found
                    if (cloneInstances)
                    {
                        allLocalizations.Add((ILocalization)(asset as ILocalization).Clone());
                    }
                    else
                    {
                        allLocalizations.Add(asset as ILocalization);
                    }
                }
            }
            // We need default language localizations
            if (!defaultFound)
            {
                Debug.Log("Add default language");
                if (cloneInstances)
                {
                    AddLanguage(defaultLanguage);
                }

#if UNITY_EDITOR
                else
                {
                    AddLanguageAndPersist(defaultLanguage);
                }
#endif
            }

        }

        /// <summary>
        /// Returns all the ids for all supported types
        /// </summary>
        /// <returns></returns>
        public List<object> GetAllIds()
        {
            List<object> res = new List<object>();
            foreach (string stringId in GetIds<string>())
            {
                res.Add(stringId);
            }
            foreach (Sprite spriteId in GetIds<Sprite>())
            {
                res.Add(spriteId);
            }
            foreach (Texture textureId in GetIds<Texture>())
            {
                res.Add(textureId);
            }
            foreach (AudioClip clipId in GetIds<AudioClip>())
            {
                res.Add(clipId);
            }
            return res;
        }

        /// <summary>
        /// Returns all the ids for a specific type.
        /// 
        /// Returns an empty list if type is not supported.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetIds<T>()
        {
            List<T> res = new List<T>();
            ILocalization defaultLocalization = GetDefaultLocalizationAsset(typeof(T));
            if(defaultLocalization != null)
            {
                foreach (object id in defaultLocalization.GetAllIds())
                {
                    res.Add((T)id);
                }
            }
            return res;
        }

        /// <summary>
        /// Add a new language.
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="persistNewAssets"></param>
        public void AddLanguage(SystemLanguage lang)
        {
            bool isDefault = lang.Equals(defaultLanguage);
            if (!languages.Contains(lang) || isDefault)
            {
                if (!isDefault)
                    languages.Add(lang);

                // New String LanguageTranslation
                StringLocalization stringLocalization = ScriptableObject.CreateInstance<StringLocalization>();
                stringLocalization.language = lang;
                allLocalizations.Add(stringLocalization);

                // New Sprite LanguageTranslation
                SpriteLocalization spriteLocalization = ScriptableObject.CreateInstance<SpriteLocalization>();
                spriteLocalization.language = lang;
                allLocalizations.Add(spriteLocalization);

                // New Texture LanguageTranslation
                TextureLocalization textureLocalization = ScriptableObject.CreateInstance<TextureLocalization>();
                textureLocalization.language = lang;
                allLocalizations.Add(textureLocalization);

                // New AudioClip LanguageTranslation
                AudioClipLocalization audioClipLocalization = ScriptableObject.CreateInstance<AudioClipLocalization>();
                audioClipLocalization.language = lang;
                allLocalizations.Add(audioClipLocalization);

            }
        }

        /// <summary>
        /// Remove a language
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public bool RemoveLanguage(SystemLanguage lang)
        {
            bool res = false;

            if (languages.Contains(lang))
            {
                languages.Remove(lang);

                List<ILocalization> toRemove = new List<ILocalization>();
                foreach (ILocalization localization in allLocalizations)
                {
                    if (localization.GetLanguage().Equals(lang))
                    {
                        toRemove.Add(localization);
                    }
                }
                foreach (ILocalization localization in toRemove)
                {
                    allLocalizations.Remove(localization);
                }

                res = true;
            }

            return res;
        }

        /// <summary>
        /// Get the localization for id and language.
        /// Returns null if the id is not localized or the language not supported.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public object GetLocalization(object id, SystemLanguage language)
        {
            object res = null;
            foreach (ILocalization localization in allLocalizations)
            {
                // search for the ILocalization for this language and this type
                if ((localization.GetLocalizationType().Equals(id.GetType()) || id.GetType().IsSubclassOf(localization.GetLocalizationType()))
                    && localization.GetLanguage().Equals(language))
                {
                    // Can return null
                    res = localization.GetLocalization(id);
                    break;
                }

            }
            return res;
        }

        /// <summary>
        /// Set the localization of id for language.
        /// 
        /// Id is created if not present.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <param name="localizationValue"></param>
        public void SetLocalization(object id, SystemLanguage language, object localizationValue)
        {
            if (!HasId(id))
            {
                AddId(id);
            }

            foreach (ILocalization localization in allLocalizations)
            {
                if ((localization.GetLocalizationType().Equals(id.GetType()) || id.GetType().IsSubclassOf(localization.GetLocalizationType()))
                    && localization.GetLanguage().Equals(language))
                {
                    localization.SetLocalization(id, localizationValue);
                }
            }
        }
        
        /// <summary>
        /// Add a new id to the localization list
        /// </summary>
        /// <param name="id"></param>
        public void AddId(object id)
        {
            if (id != null && !HasId(id))
            {
                ILocalization defaultLocalization = GetDefaultLocalizationAsset(id.GetType());
                defaultLocalization.SetLocalization(id, null);
            }
        }

        /// <summary>
        /// Removes an id and its localizations
        /// </summary>
        /// <param name="id"></param>
        public void RemoveId(object id)
        {
            if(id != null && HasId(id))
            {
                // Remove from default
                ILocalization defaultLocalization = GetDefaultLocalizationAsset(id.GetType());
                defaultLocalization.RemoveLocalization(id);

                // Remove localizations, if exist
                foreach (ILocalization localization in allLocalizations)
                {
                    if ((localization.GetLocalizationType().Equals(id.GetType()) || id.GetType().IsSubclassOf(localization.GetLocalizationType())))
                    {
                        localization.RemoveLocalization(id);
                    }
                }
            }
        }

        /// <summary>
        /// Replace an Id, all translations are kept, only the id changes
        /// </summary> 
        /// <param name="oldId"></param>
        /// <param name="newId"></param>
        public void ReplaceId(object oldId, object newId)
        {

            if (HasId(oldId) && !HasId(newId))
            {
                // Replace in default
                ILocalization defaultLocalization = GetDefaultLocalizationAsset(oldId.GetType());
                defaultLocalization.ReplaceId(oldId, newId);

                // Replace in each translation the newId
                foreach (ILocalization localization in allLocalizations)
                {
                    if (localization.GetLocalizationType().Equals(oldId.GetType()) || oldId.GetType().IsSubclassOf(localization.GetLocalizationType()))
                    {
                        localization.ReplaceId(oldId, newId);
                    }
                }
            }
        }

        /// <summary>
        /// Test if id is present.
        /// 
        /// Id can be present but with no localization.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasId(object id)
        {
            if(id == null)
            {
                return false;
            }

            ILocalization localization = GetDefaultLocalizationAsset(id.GetType());
            if(localization == null)
            {
                // should never happen...
                return false;
            }

            if (id is string)
            {
                List<string> localizationIds = new List<string>();
                foreach (object localizationId in localization.GetAllIds())
                {
                    localizationIds.Add((string)localizationId);
                }
                return localizationIds.Contains((string)id, StringComparer.OrdinalIgnoreCase);
            }
            else if (id is Sprite)
            {
                List<Sprite> localizationIds = new List<Sprite>();
                foreach (object localizationId in localization.GetAllIds())
                {
                    localizationIds.Add((Sprite)localizationId);
                }
                return localizationIds.Contains((Sprite)id);
            }
            else if (id is Texture)
            {
                List<Texture> localizationIds = new List<Texture>();
                foreach (object localizationId in localization.GetAllIds())
                {
                    localizationIds.Add((Texture)localizationId);
                }
                return localizationIds.Contains((Texture)id);
            }
            else if (id is AudioClip)
            {
                List<AudioClip> localizationIds = new List<AudioClip>();
                foreach (object localizationId in localization.GetAllIds())
                {
                    localizationIds.Add((AudioClip)localizationId);
                }
                return localizationIds.Contains((AudioClip)id);
            }

            return false;
        }

        /// <summary>
        /// Returns the ILocalization for the localizationType and the default language
        /// </summary>
        /// <param name="localizationType"></param>
        /// <returns></returns>
        public ILocalization GetDefaultLocalizationAsset (Type localizationType)
        {
            return GetLocalizationAsset(localizationType, defaultLanguage);
        }

        /// <summary>
        /// Returns the ILocalization for the localizationType and language.
        /// </summary>
        /// <param name="localizationType"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public ILocalization GetLocalizationAsset(Type localizationType, SystemLanguage language)
        {
            ILocalization res = null;

            foreach (ILocalization localization in allLocalizations)
            {
                if ((localization.GetLocalizationType().Equals(localizationType) || localizationType.IsSubclassOf(localization.GetLocalizationType()))
                    && localization.GetLanguage().Equals(language))
                {
                    res = localization;
                    break;
                }
            }

            return res;
        }

        public object Clone()
        {
            LocalizationSetup res = ScriptableObject.CreateInstance<LocalizationSetup>();
            
            res.allLocalizations = new List<ILocalization>();
            foreach (ILocalization localization in this.allLocalizations)
            {
                res.allLocalizations.Add((ILocalization) localization.Clone());
            }

            res.languages = new List<SystemLanguage>();
            foreach (SystemLanguage language in this.languages)
            {
                res.languages.Add(language);
            }
            res.active = this.active;
            res.defaultLanguage = this.defaultLanguage;
            res.forceLanguage = this.forceLanguage;
            res.forcedLanguage = this.forcedLanguage;
            res.useDefaultLanguageForNullValues = this.useDefaultLanguageForNullValues;

            return res;
        }

        // Editor only functions, persist on disk.

#if UNITY_EDITOR

        /// <summary>
        /// Editor only method persisting sub assets
        /// </summary>
        public void PersistSubAssets()
        {
            String path = setupPersistPath + Path.DirectorySeparatorChar + setupPersistName;
            // Destroy sub assets to recreate them
            UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);

            // => trigger serialization (data ready)
            // create sub assets
            foreach (ILocalization trans in allLocalizations)
            {
                bool exists = false;
                foreach (var o in objects)
                {
                    if (o == (UnityEngine.Object)trans)
                    {
                        exists = true;
                    }
                }
                if (trans is StringLocalization)
                {
                    if (!exists)
                    {
                        AssetDatabase.AddObjectToAsset((StringLocalization)trans, this);
                        // Reimport the asset after adding an object.
                        // Otherwise the change only shows up when saving the project
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((StringLocalization)trans));
                    }

                }
                else if (trans is SpriteLocalization)
                {
                    if (!exists)
                    {
                        AssetDatabase.AddObjectToAsset((SpriteLocalization)trans, this);
                        // Reimport the asset after adding an object.
                        // Otherwise the change only shows up when saving the project
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((SpriteLocalization)trans));
                    }
                }
                else if (trans is TextureLocalization)
                {
                    if (!exists)
                    {
                        AssetDatabase.AddObjectToAsset((TextureLocalization)trans, this);
                        // Reimport the asset after adding an object.
                        // Otherwise the change only shows up when saving the project
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((TextureLocalization)trans));
                    }
                }
                else if (trans is AudioClipLocalization)
                {
                    if (!exists)
                    {
                        AssetDatabase.AddObjectToAsset((AudioClipLocalization)trans, this);
                        // Reimport the asset after adding an object.
                        // Otherwise the change only shows up when saving the project
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((AudioClipLocalization)trans));
                    }
                }
            }

            // Apply subasset modification by reloading main asset
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));

        }

        public void SetDefaultLanguage(SystemLanguage newDefault)
        {
            if (!defaultLanguage.Equals(newDefault) && !languages.Contains(newDefault))
            {
                ILocalization defaultLocalization = null;

                defaultLocalization = GetDefaultLocalizationAsset(typeof(string));
                List<object> oldStringIds = defaultLocalization.GetAllIds();
                allLocalizations.Remove(defaultLocalization);
                // Destroy old default asset
                DestroyImmediate((StringLocalization)defaultLocalization, true);

                defaultLocalization = GetDefaultLocalizationAsset(typeof(Sprite));
                List<object> oldSpriteIds = defaultLocalization.GetAllIds();
                allLocalizations.Remove(defaultLocalization);
                // Destroy old default asset
                DestroyImmediate((SpriteLocalization)defaultLocalization, true);

                defaultLocalization = GetDefaultLocalizationAsset(typeof(Texture));
                List<object> oldTextureIds = defaultLocalization.GetAllIds();
                allLocalizations.Remove(defaultLocalization);
                // Destroy old default asset
                DestroyImmediate((TextureLocalization)defaultLocalization, true);

                defaultLocalization = GetDefaultLocalizationAsset(typeof(AudioClip));
                List<object> oldAudioClipIds = defaultLocalization.GetAllIds();
                allLocalizations.Remove(defaultLocalization);
                // Destroy old default asset
                DestroyImmediate((AudioClipLocalization)defaultLocalization, true);

                // Apply subasset modification by reloading main asset
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));

                // Change default
                defaultLanguage = newDefault;
                AddLanguageAndPersist(newDefault);

                // Now reimport all ids in the new default language
                defaultLocalization = GetDefaultLocalizationAsset(typeof(string));
                foreach (object id in oldStringIds)
                {
                    defaultLocalization.SetLocalization(id, null);
                }
                defaultLocalization = GetDefaultLocalizationAsset(typeof(Sprite));
                foreach (object id in oldSpriteIds)
                {
                    defaultLocalization.SetLocalization(id, null);
                }
                defaultLocalization = GetDefaultLocalizationAsset(typeof(Texture));
                foreach (object id in oldTextureIds)
                {
                    defaultLocalization.SetLocalization(id, null);
                }
                defaultLocalization = GetDefaultLocalizationAsset(typeof(AudioClip));
                foreach (object id in oldAudioClipIds)
                {
                    defaultLocalization.SetLocalization(id, null);
                }
            }
        }

        /// <summary>
        /// Editor only method
        /// Add a new supported language and persist newly created assets
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="persistNewAssets"></param>
        public void AddLanguageAndPersist(SystemLanguage lang)
        {
            bool isDefault = lang.Equals(defaultLanguage);
            if (!languages.Contains(lang))
            {
                if (!isDefault)
                    languages.Add(lang);
                 
                // New String LanguageTranslation
                StringLocalization stringLocalization = ScriptableObject.CreateInstance<StringLocalization>();
                stringLocalization.language = lang;
                allLocalizations.Add(stringLocalization);

                AssetDatabase.AddObjectToAsset((StringLocalization)stringLocalization, this);
                // Reimport the asset after adding an object.
                // Otherwise the change only shows up when saving the project
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((StringLocalization)stringLocalization));

                // New Sprite LanguageTranslation
                SpriteLocalization spriteLocalization = ScriptableObject.CreateInstance<SpriteLocalization>();
                spriteLocalization.language = lang;
                allLocalizations.Add(spriteLocalization);

                AssetDatabase.AddObjectToAsset((SpriteLocalization)spriteLocalization, this);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((SpriteLocalization)spriteLocalization));

                // New Texture LanguageTranslation
                TextureLocalization textureLocalization = ScriptableObject.CreateInstance<TextureLocalization>();
                textureLocalization.language = lang;
                allLocalizations.Add(textureLocalization);

                AssetDatabase.AddObjectToAsset((TextureLocalization)textureLocalization, this);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((TextureLocalization)textureLocalization));

                // New AudioClip LanguageTranslation
                AudioClipLocalization audioClipLocalization = ScriptableObject.CreateInstance<AudioClipLocalization>();
                audioClipLocalization.language = lang;
                allLocalizations.Add(audioClipLocalization);

                AssetDatabase.AddObjectToAsset((AudioClipLocalization)audioClipLocalization, this);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((AudioClipLocalization)audioClipLocalization));

            }
        }

        public bool RemoveLanguageAndPersist(SystemLanguage lang)
        {
            bool res = false;

            if (languages.Contains(lang))
            {
                languages.Remove(lang);

                List<ILocalization> toRemove = new List<ILocalization>();
                foreach (ILocalization localization in allLocalizations)
                {
                    if (localization.GetLanguage().Equals(lang))
                    {
                        toRemove.Add(localization);
                    }
                }
                foreach (ILocalization localization in toRemove)
                {
                    allLocalizations.Remove(localization);

                    // Destroy asset
                    if (localization is StringLocalization)
                        DestroyImmediate((StringLocalization)localization, true);
                    if (localization is SpriteLocalization)
                        DestroyImmediate((SpriteLocalization)localization, true);
                    if (localization is TextureLocalization)
                        DestroyImmediate((TextureLocalization)localization, true);
                    if (localization is AudioClipLocalization)
                        DestroyImmediate((AudioClipLocalization)localization, true);

                }

                // Apply subasset modification by reloading main asset
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));

                res = true;
            }

            return res;
        }

#endif
    }

}