// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

internal class Program
{
    /// <summary>
    /// Entry point for the MCP server application.
    /// Sets up logging, configures the MCP server with standard I/O transport and tool discovery,
    /// builds the host, and runs the server asynchronously.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    private static async Task Main(string[] args)
    {
        // Create the application host builder
        var builder = Host.CreateApplicationBuilder(args);

        // Configure console logging with Trace level
        _ = builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Register ISqlConnectionFactory and Tools for DI
        _ = builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        _ = builder.Services.AddSingleton<Tools>();

        // Check if READONLY mode is enabled
        bool isReadOnlyMode = Environment.GetEnvironmentVariable("READONLY")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        // Register MCP server and tools
        var mcpServerBuilder = builder.Services
            .AddMcpServer()
            .WithStdioServerTransport();

        if (isReadOnlyMode)
        {
            // Load only ReadOnly tools when in ReadOnly mode
            var readOnlyTools = new List<McpServerTool>
            {
                McpServerTool.Create(typeof(Tools).GetMethod("TestConnection")!, typeof(Tools), new() { Services = null }),
                McpServerTool.Create(typeof(Tools).GetMethod("DescribeTable")!, typeof(Tools), new() { Services = null }),
                McpServerTool.Create(typeof(Tools).GetMethod("ListTables")!, typeof(Tools), new() { Services = null }),
                McpServerTool.Create(typeof(Tools).GetMethod("ReadData")!, typeof(Tools), new() { Services = null })
            };
            mcpServerBuilder.WithTools(readOnlyTools);
        }
        else
        {
            // Load all tools from assembly when not in ReadOnly mode
            mcpServerBuilder.WithToolsFromAssembly(typeof(Tools).Assembly);
        }

        // Build the host
        var host = builder.Build();

        // Setup cancellation token for graceful shutdown (Ctrl+C or SIGTERM)
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true; // Prevent the process from terminating immediately
            cts.Cancel();
        };

        try
        {
            // Run the host with cancellation support
            await host.RunAsync(cts.Token);
        }
        catch (Exception ex)
        {
            // Attempt to log the exception using the host's logger
            if (host.Services.GetService(typeof(ILogger<Program>)) is ILogger<Program> logger)
            {
                logger.LogCritical(ex, "Unhandled exception occurred during host execution.");
            }
            else
            {
                Console.Error.WriteLine($"Unhandled exception: {ex}");
            }

            // Set a non-zero exit code
            Environment.ExitCode = 1;
        }
    }
}
