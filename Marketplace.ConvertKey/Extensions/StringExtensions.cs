using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Import
{
    internal static class StringExtensions
    {
        public static string JoinString<TSourse>(this IEnumerable<TSourse> sourses, string separator = ",") =>
            sourses.Select(x => x.ToString()).JoinString(separator);

        public static string JoinString(this IEnumerable<string> sourses, string separator = ",") =>
             sourses.ToArray().JoinString(separator);

        public static string JoinString(this string[] sourses, string separator = ",") =>
            string.Join(separator, sourses);

        public static bool GetBoolParam(this Uri uri, string key, bool req = false) =>
            uri.ExecuteParam(key, bool.Parse, req);

        public static int GetIntParam(this Uri uri, string key, bool req = false) =>
            uri.ExecuteParam(key, int.Parse, req);

        public static Guid GetGuidParam(this Uri uri, string key, bool req = false) =>
            uri.ExecuteParam(key, x => new Guid(x), req);

        public static string GetStringParam(this Uri uri, string key, bool req = false) =>
            uri.ExecuteParam(key, x => x, req);

        private static T ExecuteParam<T>(this Uri uri, string key, Func<string, T> func, bool req = false)
        {
            string value = uri.GetParam(key);
            if (string.IsNullOrEmpty(value))
            {
                if (req)
                    throw new Exception($"Не передан обязательный параметр '{key}'");
                return default;
            }
            try
            {
                return func(value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении параметра '{key}' из значения '{value}'", ex);
            }
        }

        public static string GetParam(this Uri uri, string key)
        {
            string keyWithSplit = $"?{key}=";
            int count;
            int index;
            if (uri.Query.StartsWith(keyWithSplit))
            {
                index = keyWithSplit.Length;
                int endIndex = uri.Query.IndexOf("&", keyWithSplit.Length);
                count = endIndex > -1 ? endIndex - keyWithSplit.Length : uri.Query.Length - index;
            }
            else
            {
                keyWithSplit = $"&{key}=";
                index = uri.Query.IndexOf(keyWithSplit);
                if (index == -1)
                    return null;

                index += keyWithSplit.Length;
                int endIndex = uri.Query.IndexOf("&", index);
                count = endIndex > -1 ? endIndex - index : uri.Query.Length - index;
            }

            string result = uri.Query.Substring(index, count);
            return result;
        }

        public static byte[] StringHexToBytes(this string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static string ToStringFromBase64Encoding(this string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return base64;

            byte[] unicode = Convert.FromBase64String(base64);
            string outPut = Encoding.Unicode.GetString(unicode);

            return outPut;
        }

        public static string ToBase64Encoding(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            byte[] unicode = Encoding.Unicode.GetBytes(input);
            string base64 = Convert.ToBase64String(unicode);

            return base64;
        }

        public static string Crypt(this string text, string keyStr)
        {
            string result = null;

            if (!String.IsNullOrEmpty(text))
            {
                byte[] inputbuffer = Encoding.Unicode.GetBytes(text);
                byte[] outputBuffer = Crypt(inputbuffer, keyStr);
                result = outputBuffer.ToHEXString();
            }

            return result;
        }

        public static byte[] Crypt(this byte[] inputbuffer, string keyStr)
        {
            byte[] result = null;

            if (inputbuffer != null)
            {
                using (SymmetricAlgorithm symmetricAlgorithm = DES.Create())
                {
                    byte[] key = GetKey(keyStr);
                    ICryptoTransform transform = symmetricAlgorithm.CreateEncryptor(key, key);
                    byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
                    return outputBuffer;
                }
            }

            return result;
        }

        public static string Decrypt(this string text, string key)
        {
            string result = null;

            if (!String.IsNullOrEmpty(text))
            {
                byte[] inputbuffer = text.StringHexToBytes();
                byte[] outputBuffer = Decrypt(inputbuffer, key);
                result = Encoding.Unicode.GetString(outputBuffer);
            }

            return result;
        }

        public static byte[] Decrypt(this byte[] inputbuffer, string keyStr)
        {
            byte[] result = null;

            if (inputbuffer != null)
            {
                using (SymmetricAlgorithm symmetricAlgorithm = DES.Create())
                {
                    byte[] key = GetKey(keyStr);
                    symmetricAlgorithm.KeySize = key.Length * 8;
                    ICryptoTransform transform = symmetricAlgorithm.CreateDecryptor(key, key);

                    try
                    {
                        byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
                        result = outputBuffer;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Не удалось расшифровать данные, похоже мастер ключ изменился", ex);
                    }
                }
            }

            return result;
        }

        private static byte[] GetKey(string key)
        {
            string keyStr = key;// ?? GetKeyString();
            return Encoding.Unicode.GetBytes(keyStr).ComputeHashSHA256().Take(8).ToArray();
        }

        internal static string GetKeyString()
        {
            string keyStr = $"{typeof(StringExtensions).FullName} {Environment.MachineName} {Environment.UserName}";
            return keyStr;
        }

        private static readonly SHA256 hashAlgorithm = SHA256.Create();

        public static string ToHEXString(this byte[] value)
        {
            if (value == null)
                return null;

            string hash = String.Empty;
            foreach (byte theByte in value)
            {
                hash += theByte.ToString("x2");
            }
            return hash;
        }

        public static byte[] ComputeHashSHA256(this byte[] value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            byte[] crypto = hashAlgorithm.ComputeHash(value);
            return crypto;
        }

        public static string ComputeHash(this byte[] value)
        {
            return value.ComputeHashSHA256().ToHEXString();
        }
    }
}
