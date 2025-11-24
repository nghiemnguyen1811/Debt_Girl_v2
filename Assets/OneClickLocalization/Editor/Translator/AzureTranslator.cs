using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using OneClickLocalization.Editor.Utils;

namespace OneClickLocalization.Editor.Translator
{
    public class AzureTranslator
    {
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";

        [Serializable]
        public class TranslationsResults
        {
            public List<TranslationResult> translationResults;
        }
        
        /// <summary>
        /// The C# classes that represents the JSON returned by the Translator Text API.
        /// </summary>
        [Serializable]
        public class TranslationResult
        {
            public DetectedLanguage detectedLanguage;
            public TextResult sourceText;
            public List<Translation> translations;
        }

        [Serializable]
        public class DetectedLanguage
        {
            public string Language;
            public float Score;
        }

        [Serializable]
        public class TextResult
        {
            public string text;
            public string script;
        }
        
        [Serializable]
        public class Translation
        {
            public string text;
            public TextResult Transliteration;
            public string to;
            public Alignment Alignment;
            public SentenceLength SentLen;
        }
        
        [Serializable]
        public class Alignment
        {
            public string Proj;
        }

        [Serializable]
        public class SentenceLength
        {
            public int[] SrcSentLen;
            public int[] TransSentLen;
        }
        
        
        private AdmAuthentication auth;
        private string azureApiKey;

        public AzureTranslator(string azureApiKey = null)
        {
            this.azureApiKey = azureApiKey;
        }

        /// <summary>
        /// Translate a single string using Azure Translator
        /// Max number of characters : 10 000.
        /// </summary>
        /// <param name="textToTranslate"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public string Translate(string textToTranslate, SystemLanguage from, SystemLanguage to)
        {
            var translationResults = Translate(new string[] {textToTranslate}, from, new SystemLanguage[]{to});
            if (translationResults == null || translationResults.translationResults.Count != 1)
                return null;

            if (translationResults.translationResults[0].translations.Count != 1)
                return null;

            return translationResults.translationResults[0].translations[0].text;
        }

        /// <summary>
        /// Translate all textsToTranslate elements using Azure Translator to all toLanguages provided
        /// </summary>
        /// <param name="textToTranslate"></param>
        /// <param name="from"></param>
        /// <param name="toLanguages"></param>
        /// <returns></returns>
        public TranslationsResults Translate(string[] toTexts, SystemLanguage from, SystemLanguage[] toLanguages)
        {
            // Verify authentication
            try
            {
                string fromCode = LanguageUtils.GetCodeFromLanguage(from);
                
                var requestBody = "[";
                foreach (var text in toTexts)
                {
                 requestBody += "{\"Text\":\"" + JsonUtils.EscapeString(text) + "\"},";   
                }
                requestBody += "]";
                
                string route = "/translate?api-version=3.0&from=" + fromCode;
                foreach (var toLanguage in toLanguages)
                {
                    route += "&to=" + LanguageUtils.GetCodeFromLanguage(toLanguage);
                }
                
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage())
                {
                    // Build the request.
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(endpoint + route);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", azureApiKey);

                    // Send the request and get response.
                    HttpResponseMessage response = client.SendAsync(request).Result;
                    // Read response as a string.
                    var responseContent = response.Content;
                    // by calling .Result you are synchronously reading the result
                    string responseString = responseContent.ReadAsStringAsync().Result;
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("Error during Azure Translator Service call : " + responseString);
                    }
                    string responseStringAsObject = JsonUtils.ArrayToObject(responseString, "translationResults");
                    TranslationsResults translationsResults = JsonUtility.FromJson<TranslationsResults>(responseStringAsObject);

                    return translationsResults;
                }

            }
            catch (Exception ex)
            {
                Debug.LogError("Error during translation : " + ex.Message);
            }

            return null;
        }

        public void SetCredentials(string azureApiKey)
        {
            if (this.azureApiKey != azureApiKey )
            {
                this.azureApiKey = azureApiKey;
            }
        }

        public class AdmAuthentication
        {
            /// URL of the token service
            private static readonly Uri AzureServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

            /// Name of header used to pass the subscription key to the token service
            private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

            public string azureApiKey;

            private string token;
            private DateTime tokenCreationDate;

            //Access token expires every 10 minutes. Renew it every 9 minutes only.
            private const int RefreshTokenDuration = 9;
            public AdmAuthentication(string azureApiKey)
            {
                this.azureApiKey = azureApiKey;
                this.token = GetAzureToken(azureApiKey);
            }

            public string GetAccessToken()
            {
                // Token has an expire time of 10 min, renew if created for more than 9 minutes
                if (this.token == null || (DateTime.Now.Subtract(tokenCreationDate) > new TimeSpan(0, 9, 0)))
                {
                    string newToken = GetAzureToken(azureApiKey);
                    this.token = newToken;
                }
                return this.token;
            }

            private void RenewAccessToken()
            {
                string newAccessToken = GetAzureToken(azureApiKey);
                //swap the new token with old one
                //Note: the swap is thread unsafe
                this.token = newAccessToken;
                tokenCreationDate = DateTime.Now;
                //Debug.Log(string.Format("Renewed token for key : {0} ", this.azureApiKey, this.token));
            }

            private string GetAzureToken(string azureApiKey)
            {
                ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
                //Prepare OAuth request 
                WebRequest webRequest = WebRequest.Create(AzureServiceUrl);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                webRequest.ContentLength = 0;

                webRequest.Headers.Add(OcpApimSubscriptionKeyHeader, azureApiKey);

                string createdToken = null;
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (var reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        createdToken = reader.ReadToEnd();
                    }
                }
                return createdToken;
            }

            /// <summary>
            /// Workaround method for security error
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="certificate"></param>
            /// <param name="chain"></param>
            /// <param name="sslPolicyErrors"></param>
            /// <returns></returns>
            public bool MyRemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                bool isOk = true;
                // If there are errors in the certificate chain, look at each error to determine the cause.
                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    for (int i = 0; i < chain.ChainStatus.Length; i++)
                    {
                        if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                        {
                            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                            chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                            bool chainIsValid = chain.Build((X509Certificate2)certificate);
                            if (!chainIsValid)
                            {
                                isOk = false;
                            }
                        }
                    }
                }
                return isOk;
            }
        }
    }

}
