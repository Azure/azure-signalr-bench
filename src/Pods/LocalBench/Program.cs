namespace LocalBench;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Contains("-h"))
        {
            PrintHelpMessage();
            return; 
        }

        var clientNum = 5; 
        var connectionString = "";
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-n" && int.TryParse(args[i + 1], out var parsedNumber))
            {
                clientNum = parsedNumber;
            }
            if (args[i] == "-c" )
            {
                if (args.Length < i + 2)
                {
                    PrintHelpMessage();
                    return;
                }
                connectionString = args[i+1];
            }
        }
        
        if(string.IsNullOrEmpty(connectionString))
        {
            PrintHelpMessage();
            return;
        }
        
        using var cts = new CancellationTokenSource();
        _= Server.RunAsync(connectionString,cts.Token);
        await Task.Delay(5000);
        
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        await Client.RunAsync(clientNum, cts.Token);
    }
    
    private static void PrintHelpMessage()
    {
        Console.WriteLine("Usage: ");
        Console.WriteLine("-h      Show this help message and exit");
        Console.WriteLine("-c \"connectionString\" [Required]  Provide the connection string of the SignalR instance");
        Console.WriteLine("-n NUM  Provide a number for concurrent client connections. Default is 5");
    }

    
}