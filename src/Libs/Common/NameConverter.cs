namespace Azure.SignalRBench.Common
{
    public class NameConverter
    {
        public static string GenerateHubName(string testId)
        {
            return "up" + testId.Replace("-", "zz");
        }

        public static string Truncate(string key)
        {
            if (key.Length <= 63)
                return key;
            var id =key+"-"+ key.GetHashCode();
            id=id.Substring(id.Length - 63);
            if (id.StartsWith("-"))
            {
                id = "t" + id.Substring(1);
            }
            return id;
        }
    }
}