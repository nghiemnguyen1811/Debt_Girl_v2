using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OneClickLocalization.Components
{
    /// <summary>
    /// Generic localization component for OCL made for ease of use and fast integreation.
    /// Automatically loalize supported components of the GameObject
    /// Supported components are : 
    /// - Text (string, "text" attribute)
    /// - TextMesh (string, "text" attribute)
    /// - TextMeshPro (string, "text" attribute)
    /// - UILabel (string, "text" attribute)
    /// - TextMeshPro (string, "text" attribute)
    /// - Image (Sprite, "sprite" attribute)
    /// - RawImage (Texture, "texture" attribute)
    /// - AudioSource (AudioClip, "clip" attribute)
    /// 
    /// To customize and add a new supported type, on the same scheme as the other types, modify InitComps and the right Get/SetValue methods.
    /// 
    /// OCLComponentAdapter check every frame if the localized attribute changed and localize it if needed.
    /// 
    /// NGUI Support must be conditionnal and handled by reflection as the package could not be present in the project.
    /// 
    /// </summary>
    [AddComponentMenu("OneClickLocalization/Component Adapter")]
    public class OCLComponentAdapter : MonoBehaviour
    {
        public bool localizeStringEnabled = true;
        public bool localizeTextureEnabled = true;
        public bool localizeSpriteEnabled = true;
        public bool localizeAudioClipEnabled = true;

        protected bool forceReset = false;

        // Text values
        protected Component textComp = null;
        protected string originalString = null;
        protected string localizedString = null;
        protected string lastTestedString = null;
        protected bool isTextLocalized = false;

        // Boolean to know if ngui is present in project
        protected bool nguiExist = false;

        // Texture values
        protected Component textureComp = null;
        protected Texture originalTexture = null;
        protected Texture localizedTexture = null;
        protected Texture lastTestedTexture = null;
        protected bool isTextureLocalized = false;

        // Image values
        protected Component spriteComp = null;
        protected Sprite originalSprite = null;
        protected Sprite localizedSprite = null;
        protected Sprite lastTestedSprite = null;
        protected bool isSpriteLocalized = false;

        // Audio values
        protected Component audioComp = null;
        protected AudioClip originalAudioClip = null;
        protected AudioClip localizedAudioClip = null;
        protected AudioClip lastTestedAudioClip = null;
        protected bool isAudioLocalized = false;

        void Start()
        {
            CheckNGuiPresence();

            InitComps();

            // init value
            CheckLocalization();
        }

        void OnEnable()
        {
            OCL.onActiveChanged += OnActiveChanged;
            OCL.onLanguageChanged += OnLanguageChanged;
            OCL.onLocalizationChanged += OnLocalizationChanged;
            OCL.onLanguagesChanged += OnLanguagesListChanged;

            // Reset value to force localization on Enable, in case localization value changed during inactive mode when events couldn't be handled
            if ((textComp == null && textureComp == null && spriteComp == null && audioComp == null) || !gameObject.activeSelf || !OCL.IsActive())
            {
                return;
            }
            ResetValue();
        }

        void OnDisable()
        {
            OCL.onActiveChanged -= OnActiveChanged;
            OCL.onLanguageChanged -= OnLanguageChanged;
            OCL.onLocalizationChanged -= OnLocalizationChanged;
            OCL.onLanguagesChanged -= OnLanguagesListChanged;
        }

        void LateUpdate()
        {
            CheckLocalization();
        }

        /// <summary>
        /// Parse gameObject components searching for supported components.
        /// </summary>
        private void InitComps()
        {
            foreach (Component comp in GetComponents(typeof(Component)))
            {
                // Text type
                if (comp is Text || comp is TextMeshPro || comp is TextMeshProUGUI || comp is TextMesh || (nguiExist && comp.GetType().ToString().Equals("UILabel")))
                {
                    textComp = comp;
                }
                // Texture type
                else if (comp is RawImage )
                {
                    textureComp = comp;
                }
                // Sprite type
                else if (comp is Image || comp is SpriteRenderer)
                {
                    spriteComp = comp;
                }
                // AudioClip type
                else if (comp is AudioSource)
                {
                    audioComp = comp;
                }
            }
        }

        private void CheckNGuiPresence()
        {
            Type myType = Type.GetType("UILabel");
            if (myType != null)
                nguiExist = true;
        }

        /// <summary>
        /// OCL Active state changed, reset value to set back the unlocalized value
        /// </summary>
        /// <param name="isActive"></param>
        private void OnActiveChanged(bool isActive)
        {
            if ((textComp == null && textureComp == null && spriteComp == null && audioComp == null) || !gameObject.activeSelf)
            {
                return;
            }

            ResetValue();
        }

        /// <summary>
        /// OCL selected language changed, reset value to localize with the new language
        /// </summary>
        /// <param name="oldLang"></param>
        /// <param name="newLang"></param>
        private void OnLanguageChanged(SystemLanguage oldLang, SystemLanguage newLang)
        {
            if ((textComp == null && textureComp == null && spriteComp == null && audioComp == null) || !gameObject.activeSelf || !OCL.IsActive())
            {
                return;
            }
            ResetValue();
        }

        /// <summary>
        /// A localization value changed, if it's for the language used, reset to relocalize
        /// </summary>
        /// <param name="id"></param>
        /// <param name="language"></param>
        /// <param name="newValue"></param>
        private void OnLocalizationChanged(object id, SystemLanguage language, object newValue)
        {

            if ((textComp == null && textureComp == null && spriteComp == null && audioComp == null) || !gameObject.activeSelf || !OCL.IsActive())
            {
                return;
            }
            if (OCL.GetLanguage().Equals(language))
            {
                ResetValue();
            }
        }

        /// <summary>
        /// OCL languages list changed, reset to relocalize in case we were using a removed language.
        /// </summary>
        private void OnLanguagesListChanged()
        {
            if ((textComp == null && textureComp == null && spriteComp == null && audioComp == null) || !gameObject.activeSelf || !OCL.IsActive())
            {
                return;
            }
            ResetValue();
        }

        /// <summary>
        /// Reset to the unlocalized value.
        /// </summary>
        private void ResetValue()
        {
            // reset to original value if it has not change and forces localization
            if (textComp != null && localizeStringEnabled)
            {
                // originalString is valid only if text is still localized from it
                string currentText = GetTextCompValue(textComp);
                if (isTextLocalized && localizedString == currentText && originalString != null)
                    SetTextCompValue(textComp, originalString);
                else
                    originalString = currentText;
            }
            if (textureComp != null && localizeTextureEnabled )
            {
                Texture currentTexture = GetTextureCompValue(textureComp);
                if (isTextureLocalized && localizedTexture == currentTexture && originalTexture != null)
                    SetTextureCompValue(textureComp, originalTexture);
                else
                    originalTexture = currentTexture;
            }
            if (spriteComp != null && localizeSpriteEnabled)
            {
                Sprite currentSprite = GetSpriteCompValue(spriteComp);
                if (isSpriteLocalized && localizedSprite == currentSprite && originalSprite != null)
                    SetSpriteCompValue(spriteComp, originalSprite);
                else
                    originalSprite = currentSprite;
            }
            if (audioComp != null && localizeAudioClipEnabled)
            {
                AudioClip currentClip = GetAudioClipCompValue(audioComp);
                if (isAudioLocalized && localizedAudioClip == currentClip && originalAudioClip != null)
                    SetAudioClipCompValue(audioComp, originalAudioClip);
                else
                    originalAudioClip = currentClip;
            }

            forceReset = true;
        }

        /// <summary>
        /// Method called every update. Optimized as much as possible to limit localization calls.
        /// </summary>
        private void CheckLocalization()
        {
            if (!OCL.IsActive())
            {
                return;
            }

            if (textComp != null && localizeStringEnabled)
            {
                LocalizeTextBasedComp(textComp, forceReset);
            }

            if (textureComp != null && localizeTextureEnabled)
            {
                LocalizeTextureBasedComp(textureComp, forceReset);
            }

            if (spriteComp != null && localizeSpriteEnabled)
            {
                LocalizeSpriteBasedComp(spriteComp, forceReset);
            }

            if (audioComp != null && localizeAudioClipEnabled)
            {
                LocalizeAudioClipBasedComp(audioComp, forceReset);
            }

            forceReset = false;

        }

        /// <summary>
        /// Localize all Text based components with the same logic
        /// 
        /// Localization is fetched only if :
        /// - text is not localized AND current value has not been tested for localization
        /// - text has changed since last localization AND current value has not been test for localization
        /// - forceReset is true
        /// </summary>
        /// <param name="targetComp"></param>
        /// <param name="forceReset"></param>
        private void LocalizeTextBasedComp(Component targetComp, bool forceReset)
        {
            string currentTextValue = GetTextCompValue(targetComp);

            if (currentTextValue == null)
                return;

            if (((!isTextLocalized || currentTextValue != localizedString) && (currentTextValue != lastTestedString)) || forceReset)
            {
                localizedString = OCL.GetLocalization(currentTextValue);
                if (localizedString != null)
                {
                    SetTextCompValue(targetComp, localizedString);
                    lastTestedString = null;
                    isTextLocalized = true;

                }
                else
                {
                    lastTestedString = currentTextValue;
                    isTextLocalized = false;
                }

                originalString = currentTextValue;
                forceReset = false;
            }
        }

        /// <summary>
        /// Localize all Sprite based components with the same logic
        /// 
        /// Localization is fetched only if :
        /// - Sprite is not localized AND current value has not been tested for localization
        /// - Sprite has changed since last localization AND current value has not been test for localization
        /// - forceReset is true
        /// </summary>
        /// <param name="targetComp"></param>
        /// <param name="forceReset"></param>
        private void LocalizeSpriteBasedComp(Component targetComp, bool forceReset)
        {
            Sprite currentSprite = GetSpriteCompValue(targetComp);

            if (currentSprite == null)
                return;

            if (((!isSpriteLocalized || currentSprite != localizedSprite) && (currentSprite != lastTestedSprite)) || forceReset)
            {
                localizedSprite = OCL.GetLocalization(currentSprite);
                if (localizedSprite != null)
                {
                    SetSpriteCompValue(targetComp, localizedSprite);
                    lastTestedSprite = null;
                    isSpriteLocalized = true;

                    originalSprite = currentSprite;
                }
                else
                {
                    lastTestedSprite = currentSprite;
                    isSpriteLocalized = false;
                }

                forceReset = false;
            }
        }

        /// <summary>
        /// Localize all Texture based components with the same logic
        /// 
        /// Localization is fetched only if :
        /// - Texture is not localized AND current value has not been tested for localization
        /// - Texture has changed since last localization AND current value has not been test for localization
        /// - forceReset is true
        /// </summary>
        /// <param name="targetComp"></param>
        /// <param name="forceReset"></param>
        private void LocalizeTextureBasedComp(Component targetComp, bool forceReset)
        {
            Texture currentTexture = GetTextureCompValue(targetComp);

            if (currentTexture == null)
                return;

            if (((!isTextureLocalized || currentTexture != localizedTexture) && (currentTexture != lastTestedTexture)) || forceReset)
            {
                localizedTexture = OCL.GetLocalization(currentTexture);
                if (localizedTexture != null)
                {
                    SetTextureCompValue(targetComp, localizedTexture);
                    lastTestedTexture = null;
                    isTextureLocalized = true;

                    originalTexture = currentTexture;
                }
                else
                {
                    lastTestedTexture = currentTexture;
                    isTextureLocalized = false;
                }

                forceReset = false;
            }
        }

        /// <summary>
        /// Localize all AudioClip based components with the same logic
        /// 
        /// Localization is fetched only if :
        /// - AudioClip is not localized AND current value has not been tested for localization
        /// - AudioClip has changed since last localization AND current value has not been test for localization
        /// - forceReset is true
        /// </summary>
        /// <param name="targetComp"></param>
        /// <param name="forceReset"></param>
        private void LocalizeAudioClipBasedComp(Component targetComp, bool forceReset)
        {
            AudioClip currentAudioClip = GetAudioClipCompValue(targetComp);

            if (currentAudioClip == null)
                return;

            if (((!isAudioLocalized || currentAudioClip != localizedAudioClip) && (currentAudioClip != lastTestedAudioClip)) || forceReset)
            {
                localizedAudioClip = OCL.GetLocalization(currentAudioClip);
                if (localizedAudioClip != null)
                {
                    SetAudioClipCompValue(targetComp, localizedAudioClip);
                    lastTestedAudioClip = null;
                    isAudioLocalized = true;

                    originalAudioClip = currentAudioClip;
                }
                else
                {
                    lastTestedAudioClip = currentAudioClip;
                    isAudioLocalized = false;
                }

                forceReset = false;
            }
        }

        /// <summary>
        /// Get the  value for a Text based component
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        protected string GetTextCompValue(Component comp)
        {
            string res = null;
            if (comp is Text)
            {
                res = ((Text)comp).text;
            }
            else if(comp is TextMeshPro)
            {
                res = ((TextMeshPro)comp).text;
            }
            else if (comp is TextMeshProUGUI)
            {
                res = ((TextMeshProUGUI)comp).text;
            }
            else if (comp is TextMesh)
            {
                res = ((TextMesh)comp).text;
            }
            else if (nguiExist && comp.GetType().ToString().Equals("UILabel"))
            {
                PropertyInfo prop = comp.GetType().GetProperty("text");
                res = (string)prop.GetValue(comp, null);
            }
            return res;
        }

        /// <summary>
        /// Set the value for a Text based component
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="stringValue"></param>
        private void SetTextCompValue(Component comp, string stringValue)
        {
            if (comp is Text)
                ((Text)comp).text = stringValue;
            else if (comp is TextMesh)
                ((TextMesh)comp).text = stringValue;
            else if (comp is TextMeshPro)
                ((TextMeshPro)comp).text = stringValue;
            else if (comp is TextMeshProUGUI)
                ((TextMeshProUGUI)comp).text = stringValue;
            else if (nguiExist && comp.GetType().ToString().Equals("UILabel"))
            {
                PropertyInfo prop = comp.GetType().GetProperty("text");
                prop.SetValue(comp, stringValue, null);
            }
        }

        /// <summary>
        /// Get the value for an AudioClip based component
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        protected AudioClip GetAudioClipCompValue(Component comp)
        {
            AudioClip res = null;
            if (comp is AudioSource)
            {
                res = ((AudioSource)comp).clip;
            }
            return res;
        }

        /// <summary>
        /// Set the value for an AudioClip based component
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="clipValue"></param>
        private void SetAudioClipCompValue(Component comp, AudioClip clipValue)
        {
            if (comp is AudioSource)
                ((AudioSource)comp).clip = clipValue;
        }

        /// <summary>
        /// Get the value for a Texture based component
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        protected Texture GetTextureCompValue(Component comp)
        {
            Texture res = null;
            if (comp is RawImage)
            {
                res = ((RawImage)comp).texture;
            }
            return res;
        }

        /// <summary>
        /// Set the value for a Texture based component
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="textureValue"></param>
        private void SetTextureCompValue(Component comp, Texture textureValue)
        {
            if (comp is RawImage)
                ((RawImage)comp).texture = textureValue;
        }

        /// <summary>
        /// Get the value for a Sprite based component
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        protected Sprite GetSpriteCompValue(Component comp)
        {
            Sprite res = null;
            if (comp is Image)
            {
                res = ((Image)comp).sprite;
            }
            return res;
        }

        /// <summary>
        /// Set the value for a Sprite based component
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="spriteValue"></param>
        private void SetSpriteCompValue(Component comp, Sprite spriteValue)
        {
            if (comp is Image)
                ((Image)comp).sprite = spriteValue;
        }
    }
}
