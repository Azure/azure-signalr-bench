using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public static class Extensions
    {
        public static bool TryGetTypedValue<TKey, TValue, TActual>(
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
    }
}
