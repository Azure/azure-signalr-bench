using System;
using System.Data.SqlClient;
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

        public Task CreateTableIfNotExist(string table, Func<string, string> genCreateTableCommand)
        {
            if (!isTableExist(table))
            {
                var commandToCreateTbl = genCreateTableCommand(table);
                CommandsToCreateTable(table);
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

        public int InsertRecord(string table, ReportRecord stat)
        {
            var ts = stat.Timestamp;
            var id = Convert.ToInt64(ts);
            var dt = Utils.ConvertFromTimestamp(ts);
            var dbId = $"{id}{stat.Scenario}";
            if (stat.HasConnectionStat)
            {
                return InsertConnStatRecord(
                        table,
                        dbId,
                        dt,
                        stat.Scenario,
                        stat.Unit(),
                        stat.Connections,
                        stat.Sends,
                        stat.SendTPuts,
                        stat.RecvTPuts,
                        stat.Reference,
                        stat.DroppedConnections,
                        stat.ReconnCost99Percent,
                        stat.LifeSpan99Percent,
                        stat.Offline99Percent,
                        stat.Others);
            }
            else
            {
                return InsertCommonRecord(
                        table,
                        dbId,
                        dt,
                        stat.Scenario,
                        stat.Unit(),
                        stat.Connections,
                        stat.Sends,
                        stat.SendTPuts,
                        stat.RecvTPuts,
                        stat.Reference);
            }
        }

        private int InsertConnStatRecord(
            string table,
            string id,
            DateTime reportDateTime,
            string scenario,
            int unit,
            int connections,
            int sends,
            long sendTPuts,
            long recvTPuts,
            string reference,
            int droppedConnections,
            int reconnCost99Percent,
            int lifeSpan99Percent,
            int offline99Percent,
            string others)
        {
            var command4Insert = $@"
        IF NOT EXISTS (SELECT * FROM {table} r WHERE r.Id = '{id}')
           INSERT INTO [dbo].[{table}] (
              [Id],
              [ReportDateTime],
              [Scenario],
              [Unit],
              [Connections],
              [Sends],
              [SendTPuts],
              [RecvTPuts],
              [Reference],
              [DroppedConnections],
              [ReconnectCost99Percent],
              [LifeSpan99Percent],
              [Offline99Percent],
              [Others]) VALUES (
              @id,
              @reportDateTime,
              @scenario,
              @unit,
              @connections,
              @sends,
              @sendTPuts,
              @recvTPuts,
              @reference,
              @droppedConnections,
              @reconnectCost99Percent,
              @lifeSpan99Percent,
              @offline99Percent,
              @others)";
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
            insertCmd.Parameters.AddWithValue("@droppedConnections", droppedConnections);
            insertCmd.Parameters.AddWithValue("@reconnectCost99Percent", reconnCost99Percent);
            insertCmd.Parameters.AddWithValue("@lifeSpan99Percent", lifeSpan99Percent);
            insertCmd.Parameters.AddWithValue("@offline99Percent", offline99Percent);
            insertCmd.Parameters.AddWithValue("@others", others);
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

        private int InsertCommonRecord(
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
        IF NOT EXISTS (SELECT * FROM {table} r WHERE r.Id = '{id}')
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

        public static string CommandsToCreateConnStatTable(string table)
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
        DroppedConnections int,
        ReconnectCost99Percent int,
        LifeSpan99Percent int,
        Offline99Percent int,
        Others varchar(4096) NOT NULL,
        CONSTRAINT PK_{table} PRIMARY KEY (Id)
    )
";
            return createTableCommand;
        }

        public static string CommandsToCreateTable(string table)
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
