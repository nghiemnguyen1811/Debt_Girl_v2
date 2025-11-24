using System;
using UnityEngine;

namespace OneClickLocalization.Editor.Utils
{
    public static class JsonUtils
    {
        /// <summary>
        /// Needed for compatibility because Unity's JsonUtility doesn't support Array based json 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ArrayToObject(string value, string propertyName)
        {
            value = "{\""+ propertyName + "\":" + value + "}";
            return value;
        }
        
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
        
        private static  bool NeedEscape(string src, int i)
        {
            char c = src[i];
            return c < 32 || c == '"' || c == '\\'
                   // Broken lead surrogate
                   || (c >= '\uD800' && c <= '\uDBFF' &&
                       (i == src.Length - 1 || src[i + 1] < '\uDC00' || src[i + 1] > '\uDFFF'))
                   // Broken tail surrogate
                   || (c >= '\uDC00' && c <= '\uDFFF' &&
                       (i == 0 || src[i - 1] < '\uD800' || src[i - 1] > '\uDBFF'))
                   // To produce valid JavaScript
                   || c == '\u2028' || c == '\u2029'
                   // Escape "</" for <script> tags
                   || (c == '/' && i > 0 && src[i - 1] == '<');
        }

        public static string EscapeString(string src)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int start = 0;
            for (int i = 0; i < src.Length; i++)
                if (NeedEscape(src, i))
                {
                    sb.Append(src, start, i - start);
                    switch (src[i])
                    {
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '/': sb.Append("\\/"); break;
                        default:
                            sb.Append("\\u");
                            sb.Append(((int)src[i]).ToString("x04"));
                            break;
                    }
                    start = i + 1;
                }
            sb.Append(src, start, src.Length - start);
            return sb.ToString();
        }
    }
}