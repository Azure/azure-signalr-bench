namespace MDMSystemLoadQueryService
{
    public interface IMDMQuery
    {
        string QueryMetrics(PlatformType platformType, SystemLoadType systemLoadType, string podName, string startTime, string endTime);
    }
}
