using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace MDMSystemLoadQueryService.Controllers
{
    [Route("mdm")]
    public class MDMServiceQueryController : Controller
    {
        private IMDMQuery _mDMQuery;

        public MDMServiceQueryController(IMDMQuery mDMQuery)
        {
            _mDMQuery = mDMQuery;
        }

        // GET mdm/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET mdm/query?platform=
        [HttpGet, Route("query")]
        public string Get(string platform, string systemLoad, string podName, string dateStart, string dateEnd)
        {
            if (!Enum.TryParse<PlatformType>(platform, out var platformType))
            {
                Console.WriteLine($"Fail to parse platform type {platform}");
            }
            if (!Enum.TryParse<SystemLoadType>(systemLoad, out var systemLoadType))
            {
                Console.WriteLine($"Fail to parse SystemLoad type {systemLoad}");
            }
            Console.WriteLine($"Query metrics for {platformType}, {systemLoadType}, {podName}, {dateStart}, {dateEnd}");
            return _mDMQuery.QueryMetrics(platformType, systemLoadType, podName, dateStart, dateEnd);
        }

        // POST mdm/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT mdm/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE mdm/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
