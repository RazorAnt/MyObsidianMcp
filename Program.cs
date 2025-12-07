using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Get vault path from command-line arguments
if (args.Length == 0)
{
    Console.Error.WriteLine("Error: Vault path is required as a command-line argument");
    Console.Error.WriteLine("Usage: MyObsidian.dll /path/to/vault");
    Environment.Exit(1);
}

string vaultPath = args[0];

// Validate vault path exists
if (!Directory.Exists(vaultPath))
{
    Console.Error.WriteLine($"Error: Vault directory not found at {vaultPath}");
    Environment.Exit(1);
}

// Set the vault path in ObsidianTools
ObsidianService.SetVaultPath(vaultPath);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
