// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Events;

namespace Steps;

/// <summary>
/// Routes user questions to either general chat or document search based on intent.
/// </summary>
public sealed class RouterStep(IChatCompletionService chatCompletion) : KernelProcessStep
{
    private readonly IChatCompletionService _chatCompletion = chatCompletion;

    [KernelFunction]
    public async ValueTask ExecuteAsync(KernelProcessStepContext context, string userQuestion)
    {
        PrintHeader("Router");

        try
        {
            Console.WriteLine($"[DEBUG Router] Calling Azure OpenAI...");

            var systemPrompt = @"You are a routing classifier for Jan Enterprise queries.
Classify the user question as either:
- GENERAL: casual conversation, general business questions about Jan Enterprise
- DOCS: questions about awards, winners, categories, rankings, years (2021/2022/2023)

Respond with ONLY one word: GENERAL or DOCS";

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userQuestion);

            var executionSettings = new AzureOpenAIPromptExecutionSettings
            {
                Temperature = 0,
                MaxTokens = 10
            };

            var response = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings
            );

            var route = response.Content?.Trim().ToUpperInvariant() ?? "GENERAL";

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"🔀 Routing decision: {route}");
            Console.ResetColor();
            Console.WriteLine();

            var eventId = route == "DOCS"
                ? ProcessEvents.RoutedToDocs
                : ProcessEvents.RoutedToGeneral;

            await context.EmitEventAsync(new KernelProcessEvent
            {
                Id = eventId,
                Data = userQuestion
            });
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Error in RouterStep:");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.ResetColor();

            await context.EmitEventAsync(new KernelProcessEvent
            {
                Id = ProcessEvents.RoutedToGeneral,
                Data = userQuestion
            });
        }
    }

    private static void PrintHeader(string agentName)
    {
        Console.BackgroundColor = ConsoleColor.DarkBlue;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{agentName}]");
        Console.ResetColor();
        Console.WriteLine();
    }
}