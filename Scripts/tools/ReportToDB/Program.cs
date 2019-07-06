using CommandLine;
using System;
using System.Threading.Tasks;

namespace ReportToDB
{
    class Program
    {
        private static Task CreateTableOption(CreateTableOption createTableOption)
        {
            var connectionString = createTableOption.SqlConnectionString;
            var table = createTableOption.TableName;
            var sqlClient = new SqlClient(connectionString);
            sqlClient.Open();
            try
            {
                if (createTableOption.TableType == 1)
                {
                    sqlClient.CreateTableIfNotExist(table, SqlClient.CommandsToCreateTable);
                }
                else if (createTableOption.TableType == 2)
                {
                    sqlClient.CreateTableIfNotExist(table, SqlClient.CommandsToCreateConnStatTable);
                }
            }
            finally
            {
                sqlClient.Close();
            }
            return Task.CompletedTask;
        }

        private static Task DropTableOption(DropTableOption dropTableOption)
        {
            var connectionString = dropTableOption.SqlConnectionString;
            var table = dropTableOption.TableName;
            var sqlClient = new SqlClient(connectionString);
            sqlClient.Open();
            try
            {
                sqlClient.DropTable(table);
            }
            finally
            {
                sqlClient.Close();
            }
            return Task.CompletedTask;
        }

        private static async Task RunInsertOption(InsertToTableOption insertOption)
        {
            var connectionString = insertOption.SqlConnectionString;
            var table = insertOption.TableName;
            var csvFileName = insertOption.StatFilePath;
            var sqlClient = new SqlClient(connectionString);
            sqlClient.Open();
            try
            {
                await CreateTableOption(insertOption);
                int insertSucc = 0;
                foreach (var stat in LoadReportRecords.GetReportRecords(csvFileName))
                {
                    var ts = stat.Timestamp;
                    var id = Convert.ToInt64(ts);
                    var dt = Utils.ConvertFromTimestamp(ts);
                    insertSucc += sqlClient.InsertRecord(table, stat);
                }
                Console.WriteLine($"Finally successfully insert {insertSucc}");
            }
            finally
            {
                sqlClient.Close();
            }
        }

        private static Task Run(string[] args)
        {
            var ret = Parser.Default
                            .ParseArguments<
                                InsertToTableOption,
                                CreateTableOption,
                                DropTableOption
                            >(args)
                            .MapResult((InsertToTableOption ops) => RunInsertOption(ops),
                                   (CreateTableOption ops) => CreateTableOption(ops),
                                   (DropTableOption ops) => DropTableOption(ops),
                                   error =>
                                   {
                                       Console.WriteLine($"Error in parsing arguments: {error}");
                                       return Task.CompletedTask;
                                   });
            return ret;
        }

        static async Task Main(string[] args)
        {
            await Run(args);
        }
    }
}
