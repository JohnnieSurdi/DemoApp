// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.Projects;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Events;
using Models;
using sk_azure_agent_demo.foundry;

namespace Steps;

/// <summary>
/// Calls Azure AI Foundry Agent with file search for document-based questions.
/// </summary>
public sealed class FoundryLocalSearchStep(FoundryClientProvider foundryProvider) : KernelProcessStep
{
    private readonly FoundryClientProvider _foundryProvider = foundryProvider;

    [KernelFunction]
    public async ValueTask ExecuteAsync(KernelProcessStepContext context, string userQuestion)
    {
        PrintHeader("Foundry Local Search Agent");

        try
        {
            Console.WriteLine($"[DEBUG] Getting agents client...");
            var agentsClient = _foundryProvider.Client.GetAgentsClient();

            Console.WriteLine($"[DEBUG] Fetching agent definition: {_foundryProvider.ResearchAgentId}");
            // Get your existing Research Agent from Foundry
            Agent definition = await agentsClient.GetAgentAsync(_foundryProvider.ResearchAgentId);
            Console.WriteLine($"[DEBUG] Agent found: {definition.Name}");

            Console.WriteLine($"[DEBUG] Creating AzureAIAgent...");
            AzureAIAgent agent = new(definition, _foundryProvider.GetClientProvider());

            Console.WriteLine($"[DEBUG] Creating thread...");
            // Create thread and send message
            AgentThread thread = await agentsClient.CreateThreadAsync();
            Console.WriteLine($"[DEBUG] Thread created: {thread.Id}");

            Console.WriteLine($"[DEBUG] Sending message to thread...");
            await agentsClient.CreateMessageAsync(thread.Id, MessageRole.User, userQuestion);

            Console.WriteLine($"[DEBUG] Invoking agent stream...");
            // Collect streaming response
            var answer = string.Empty;
            await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(thread.Id))
            {
                foreach (var item in response.Items)
                {
                    if (item is StreamingTextContent textContent)
                    {
                        Console.Write(textContent);
                        answer += textContent.Text;
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine($"[DEBUG] Stream completed. Answer length: {answer.Length}");
            Console.WriteLine();

            var agentResponse = new AgentResponse(
                userQuestion,
                answer,
                "FoundryLocalSearch (Azure AI Foundry)"
            );

            Console.WriteLine($"[DEBUG] Emitting DraftReady event...");
            await context.EmitEventAsync(new KernelProcessEvent
            {
                Id = ProcessEvents.DraftReady,
                Data = agentResponse
            });
            Console.WriteLine($"[DEBUG] Event emitted successfully");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Error in FoundryLocalSearchStep:");
            Console.WriteLine($"   Type: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"   Inner Message: {ex.InnerException.Message}");
            }
            Console.WriteLine($"   Stack: {ex.StackTrace}");
            Console.ResetColor();

            // Still emit an event so the process can continue
            var errorResponse = new AgentResponse(
                userQuestion,
                $"Error: {ex.Message}",
                "FoundryLocalSearch (Error)"
            );

            await context.EmitEventAsync(new KernelProcessEvent
            {
                Id = ProcessEvents.DraftReady,
                Data = errorResponse
            });
        }
    }

    private static void PrintHeader(string agentName)
    {
        Console.BackgroundColor = ConsoleColor.DarkMagenta;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{agentName}]");
        Console.ResetColor();
        Console.WriteLine();
    }
}