using System;
using System.Linq.Expressions;
using Azure.Data.Tables;

namespace Azure.SignalRBench.Storage
{
    public static class FilterExpression
    {
        public static string All()
        {
            return "";
        }
        
        public static string KeyEqual(string key, string value)
        {
            return $"{key} eq '{value}'";
        }

        public static string Expression<T>(Expression<Func<T, bool>> expression)
        {
            return TableClient.CreateQueryFilter(expression);
        }
    }
}