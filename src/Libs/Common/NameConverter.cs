namespace Azure.SignalRBench.Common
{
    public class NameConverter
    {
        const int SafeNameLength = 63 - 11; // 63 is the max length of label/service, 11 is the length of the label suffix used in statefulset
        public static string GenerateHubName(string testId)
        {
            return "up" + testId.Replace("-", "zz");
        }

        public static string Truncate(string key)
        {
            if (key.Length <= SafeNameLength)
                return key;
            var id =key+"-"+ key.GetHashCode();
            id=id.Substring(id.Length - SafeNameLength);
            if (id.StartsWith("-"))
            {
                id = "t" + id.Substring(1);
            }
            return id;
        }
    }
}