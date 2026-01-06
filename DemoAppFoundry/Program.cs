// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Events;
using Steps;
using sk_azure_agent_demo.foundry;
using System.Diagnostics;

namespace HybridMAS
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // Force console to appear
            Console.WriteLine("=== Application Starting ===");
            Console.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
            Console.WriteLine();



            try
            {
                // Load configuration
                var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

                Console.WriteLine("✓ Configuration loaded");

                // Setup Foundry provider with connection string
                var foundryConnectionString = configuration["AzureFoundry:ConnectionString"]!;
                var researchAgentId = configuration["AzureFoundry:ResearchAgentId"]!;
                var foundryApiKey = configuration["AzureFoundry:ApiKey"]!;

                Console.WriteLine($"✓ Foundry Connection String: {foundryConnectionString.Substring(0, 50)}...");
                Console.WriteLine($"✓ Research Agent ID: {researchAgentId}");

                // Setup Foundry provider with service principal authentication
                var foundryTenantId = configuration["AzureFoundry:TenantId"]!;
                var foundryClientId = configuration["AzureFoundry:ClientId"]!;
                var foundryClientSecret = configuration["AzureFoundry:ClientSecret"]!;

                // Around line 40-52 in Program.cs, replace the Foundry provider creation with:

                // Try endpoint-based initialization
                var foundryProvider = FoundryClientProvider.Create(
    connectionString: configuration["AzureFoundry:ConnectionString"]!,
    researchAgentId: configuration["AzureFoundry:ResearchAgentId"]!,
    tenantId: configuration["AzureFoundry:TenantId"],
    clientId: configuration["AzureFoundry:ClientId"],
    clientSecret: configuration["AzureFoundry:ClientSecret"]
);
                Console.WriteLine("✓ Foundry provider created with Service Principal auth (endpoint-based)");

                // Setup Semantic Kernel with Azure OpenAI and register dependencies
                var kernelBuilder = Kernel.CreateBuilder();

                // Register all dependencies that steps need
                kernelBuilder.Services.AddSingleton(foundryProvider);

                // Register a factory for Kernel so it can be injected
                //kernelBuilder.Services.AddSingleton<Kernel>(sp =>
                //{
                //    var builder = Kernel.CreateBuilder();
                //    builder.Services.AddSingleton(foundryProvider);
                //    builder.AddAzureOpenAIChatCompletion(
                //        deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
                //        endpoint: configuration["AzureOpenAI:Endpoint"]!,
                //        apiKey: configuration["AzureOpenAI:ApiKey"]!
                //    );
                //    return builder.Build();
                //});

                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
                    endpoint: configuration["AzureOpenAI:Endpoint"]!,
                    apiKey: configuration["AzureOpenAI:ApiKey"]!
                );

                Kernel kernel = kernelBuilder.Build();
                Console.WriteLine("✓ Kernel built");

                // Build the process once
                ProcessBuilder process = new("HybridMAS");
                var init = process.AddStepFromType<Init>();
                var router = process.AddStepFromType<RouterStep>();
                var generalChat = process.AddStepFromType<GeneralChatStep>();
                var foundrySearch = process.AddStepFromType<FoundryLocalSearchStep>();
                var editor = process.AddStepFromType<EditorStep>();

                // Define event-driven flow
                process
                    .OnInputEvent(ProcessEvents.StartProcess)
                    .SendEventTo(new ProcessFunctionTargetBuilder(init));

                init
                    .OnEvent(ProcessEvents.Start)
                    .SendEventTo(new ProcessFunctionTargetBuilder(router));

                router
                    .OnEvent(ProcessEvents.RoutedToGeneral)
                    .SendEventTo(new ProcessFunctionTargetBuilder(generalChat));

                router
                    .OnEvent(ProcessEvents.RoutedToDocs)
                    .SendEventTo(new ProcessFunctionTargetBuilder(foundrySearch));

                generalChat
                    .OnEvent(ProcessEvents.DraftReady)
                    .SendEventTo(new ProcessFunctionTargetBuilder(editor));

                foundrySearch
                    .OnEvent(ProcessEvents.DraftReady)
                    .SendEventTo(new ProcessFunctionTargetBuilder(editor));

                editor
                    .OnFunctionResult()
                    .StopProcess();

                KernelProcess kernelProcess = process.Build();
                Console.WriteLine("✓ Process built");
                Console.WriteLine();

                // Main interaction loop
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("\nEnter your question (or 'exit' to quit): ");
                    Console.ResetColor();

                    string? input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "exit")
                    {
                        break;
                    }

                    Console.WriteLine($"\n[DEBUG] Starting process for: {input}");

                    try
                    {
                        // Start the process for this question
                        using var runningProcess = await kernelProcess.StartAsync(
                            kernel,
                            new KernelProcessEvent()
                            {
                                Id = ProcessEvents.StartProcess,
                                Data = input
                            });

                        Console.WriteLine("[DEBUG] Process started, waiting for completion...");

                        // Wait longer for process to complete
                        await Task.Delay(30000); // 30 seconds

                        Console.WriteLine("[DEBUG] Process completed or timed out");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n❌ Error processing question: {ex.GetType().Name}");
                        Console.WriteLine($"   Message: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"   Inner: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                        }
                        Console.WriteLine($"\n   Stack: {ex.StackTrace}");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine("\nGoodbye!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Fatal Error: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\nInner Exception: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"   Message: {ex.InnerException.Message}");
                    Console.WriteLine($"   Stack:\n{ex.InnerException.StackTrace}");
                }
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}