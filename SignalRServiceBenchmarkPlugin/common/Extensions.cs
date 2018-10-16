using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public static class Extensions
    {
        private static bool TryGetTypedValueInternal<TKey, TValue, TActual>(
        this IDictionary<TKey, TValue> data,
        TKey key,
        out TActual value, Func<TValue, TActual> converter = null) where TActual : TValue
        {
            TValue tmp;
            if (data.TryGetValue(key, out tmp))
            {
                if (converter != null)
                {
                    value = converter(tmp);
                    return true;
                }
                if (tmp is TActual)
                {
                    value = (TActual)tmp;
                    return true;
                }
                value = default(TActual);
                return false;
            }
            value = default(TActual);
            return false;
        }

        public static void TryGetTypedValue<TKey, TValue, TActual>(this IDictionary<TKey, TValue> dict, TKey key, out TActual val, Func<TValue, TActual> converter = null) where TActual : TValue
        {
            var success = dict.TryGetTypedValueInternal(key, out val, converter);
            HandleGetValueResult(success, key.ToString());
        }

        private static void HandleGetValueResult(bool success, string key)
        {
            if (!success)
            {
                var message = $"Parameter {key} does not exist.";
                Log.Error(message);
                throw new Exception(message);
            }
        }

        public static string GetContents<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            var list = dict.Select(entry => $"{entry.Key} : {entry.Value}");
            return string.Join(Environment.NewLine, list);
        }
    }
}
