using System;

namespace Marketplace.Import.Helpers
{
    internal static class DictionaryParceValueExtensions
    {
        public static bool TryGetValue(this IDictionaryParceValue dic, string key, out int value) =>
             TryGetValue(dic, key, out value, int.Parse);

        public static bool TryGetValue(this IDictionaryParceValue dic, string key, out bool value) =>
             TryGetValue(dic, key, out value, bool.Parse);

        public static bool TryGetValue<T>(this IDictionaryParceValue dic, string key, out T value, Func<string, T> parcer)
        {
            if (!dic.TryGetValue(key, out string valueStr) || string.IsNullOrEmpty(valueStr))
            {
                value = default;
                return false;
            }

            try
            {
                value = parcer(valueStr);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при чтении значения '{}'", ex); ;
            }
        } 
    }
}
