using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("teststatus")]
    [ApiController]
    public class TestStatusContorller : ControllerBase
    {
        private IPerfStorage _perfStorage;

        public TestStatusContorller(IPerfStorage perfStorage)
        {
            _perfStorage = perfStorage;
        }

        [HttpGet]
        public async Task<IEnumerable<TestStatusEntity>> Get()
        {
            try
            {
            var table = await _perfStorage.GetTableAsync<TestStatusEntity>(Constants.TableNames.TestStatus);
            var rows= await table.QueryAsync(table.Rows
                                                   ).ToListAsync();
            return rows;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

    }
}
