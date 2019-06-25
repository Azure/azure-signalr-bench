using CommandLine;

namespace ReportToDB
{
    public class CommonArgsOption
    {
        [Option("SqlConnectionString", Required = true, HelpText = "Specify the SQL server connection string")]
        public string SqlConnectionString { get; set; }

        [Option("TableName", Required = false, Default = "AzureSignalRPerf", HelpText = "Specify the database table name, default is 'AzureSignalRPerf'")]
        public string TableName { get; set; }
    }

    [Verb("createTable", HelpText = "Create a table")]
    public class CreateTableOption : CommonArgsOption
    {
    }

    [Verb("dropTable", HelpText = "Drop a table")]
    public class DropTableOption : CommonArgsOption
    {
    }

    [Verb("insertRecords", HelpText = "Insert a series of records")]
    public class InsertToTableOption : CommonArgsOption
    {
        [Option("InputFile", Required = false, Default = "table.csv", HelpText = "Specify the CSV file of report data")]
        public string StatFilePath { get; set; }
    }
}
