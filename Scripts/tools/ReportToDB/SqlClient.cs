using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace ReportToDB
{
    public class SqlClient
    {
        public SqlConnection _sqlConnection;

        public SqlClient(string connectionString)
        {
            _sqlConnection = new SqlConnection(connectionString);
        }

        public Task CreateTableIfNotExist(string table)
        {
            if (!isTableExist(table))
            {
                var commandToCreateTbl = CommandsToCreateTable(table);
                using (var command = new SqlCommand(commandToCreateTbl, _sqlConnection))
                {
                    var ret = command.ExecuteScalar();
                    Console.WriteLine($"{ret}");
                }
            }
            return Task.CompletedTask;
        }

        public Task DropTable(string table)
        {
            if (isTableExist(table))
            {
                var commandToDropTbl = CommandsToDropTable(table);
                using (var command = new SqlCommand(commandToDropTbl, _sqlConnection))
                {
                    var ret = command.ExecuteScalar();
                    if (ret == null)
                    {
                        Console.WriteLine($"successfully drop table {table}");
                    }
                }
            }
            return Task.CompletedTask;
        }
        
        public void Open()
        {
            _sqlConnection.Open();
        }

        public void Close()
        {
            _sqlConnection.Close();
        }

        private IEnumerable<ReportRecord> FilterNewRecord(
            IEnumerable<ReportRecord> plannedRecords,
            string table,
            DateTime latestRecordInDB)
        {
            var commandToGetLatestRecord = $@"
            
";
            return plannedRecords.Where(x => {
                var dt = Utils.ConvertFromTimestamp(x.Timestamp);
                return dt > latestRecordInDB;
            });
        }

        public int InsertRecord(
            string table,
            string id,
            DateTime reportDateTime,
            string scenario,
            int unit,
            int connections,
            int sends,
            long sendTPuts,
            long recvTPuts,
            string reference)
        {

            var command4Insert = $@"
        IF NOT EXISTS (SELECT * FROM AzureSignalRPerf r WHERE r.Id = '{id}')
           INSERT INTO [dbo].[{table}] (
              [Id],
              [ReportDateTime],
              [Scenario],
              [Unit],
              [Connections],
              [Sends],
              [SendTPuts],
              [RecvTPuts],
              [Reference]) VALUES (
              @id,
              @reportDateTime,
              @scenario,
              @unit,
              @connections,
              @sends,
              @sendTPuts,
              @recvTPuts,
              @reference)";
            var insertCmd = new SqlCommand(command4Insert, _sqlConnection);
            insertCmd.Parameters.AddWithValue("@id", id);
            insertCmd.Parameters.AddWithValue("@reportDateTime", reportDateTime);
            insertCmd.Parameters.AddWithValue("@scenario", scenario);
            insertCmd.Parameters.AddWithValue("@unit", unit);
            insertCmd.Parameters.AddWithValue("@connections", connections);
            insertCmd.Parameters.AddWithValue("@sends", sends);
            insertCmd.Parameters.AddWithValue("@sendTPuts", sendTPuts);
            insertCmd.Parameters.AddWithValue("@recvTPuts", recvTPuts);
            insertCmd.Parameters.AddWithValue("@reference", reference);
            try
            {
                var count = insertCmd.ExecuteNonQuery();
                if (count == -1)
                {
                    return 0;
                }
                return count;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
            }
            return -1;
        }

        private bool isTableExist(string table)
        {
            var commands4isTblExist = CommandsToCheckTableExist(table);
            using (var command = new SqlCommand(commands4isTblExist, _sqlConnection))
            {
                int ret = (int)command.ExecuteScalar();
                if (ret == 1)
                {
                    Console.WriteLine($"table {table} exists");
                    return true;
                }
                else
                {
                    Console.WriteLine($"table {table} does not exist");
                    return false;
                }
            }
        }

        private static string CommandsToInsertRecord(
            string table,
            long id,
            DateTime reportDateTime,
            string scenario,
            int unit,
            int connections,
            int sends,
            long sendTPuts,
            long recvTPuts)
        {
            var command = $@"
        INSERT INTO [dbo].[{table}] (
              [Id],
              [ReportDateTime],
              [Scenario],
              [Unit],
              [Connections],
              [Sends],
              [SendTPuts],
              [RecvTPuts],
              [Reference]) VALUES (
              @id,
              @reportDateTime,
              @scenario,
              @unit,
              @connections,
              @sends,
              @sendTPuts,
              @recvTPuts,
              @reference)
";
            return command;
        }

        private static string CommandsToCheckTableExist(string table)
        {
            var command = $@"
                SELECT Count(*) FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = '{table}'";
            return command;
        }

        private static string CommandsToDropTable(string table)
        {
            var dropTableCommand = $@"DROP TABLE dbo.{table}";
            return dropTableCommand;
        }

        private static string CommandsToCreateTable(string table)
        {
            var createTableCommand = $@"
    CREATE TABLE {table} (
        Id varchar(128) NOT NULL,
        ReportDateTime DateTime NOT NULL,
        Scenario varchar(255) NOT NULL,
        Unit int,
        Connections int,
        Sends int,
        SendTPuts bigint,
        RecvTPuts bigint,
        Reference varchar(512),
        CONSTRAINT PK_{table} PRIMARY KEY (Id)
    )
";
            return createTableCommand;
        }
    }
}
