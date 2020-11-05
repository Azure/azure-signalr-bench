using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Mvc;
using Portal.Entities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("teststatus")]
    [ApiController]
    public class TestStatusContorller : ControllerBase
    {
        private PerfStorage _perfStorage;

        public TestStatusContorller(PerfStorage perfStorage)
        {
            _perfStorage = perfStorage;
        }

        [HttpGet]
        public async Task<IEnumerable<TestStatusEntity>> Get()
        {
            var table = await _perfStorage.GetTableAsync<TestStatusEntity>(Constants.TableNames.TestStatus);
            var rows= await table.QueryAsync(table.Rows
                                                   ).ToListAsync();
            return rows;
        }

    }
}
