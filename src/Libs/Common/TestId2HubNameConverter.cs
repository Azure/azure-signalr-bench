namespace Azure.SignalRBench.Common
{
    public class TestId2HubNameConverter
    {
        public static string GenerateHubName(string testId)
        {
            return "up" + testId.Replace("-", "zz");
        }
    }
}