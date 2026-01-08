using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using DemoAppFoundry.Events;
using DemoAppFoundry.Models;

namespace DemoAppFoundry.Steps;

public sealed class GeneralChatStep(IChatCompletionService chatCompletion) : KernelProcessStep
{
    private readonly IChatCompletionService _chatCompletion = chatCompletion;

    [KernelFunction]
    public async ValueTask ExecuteAsync(KernelProcessStepContext context, string userQuestion)
    {
        PrintHeader("General Chat Agent");

        var systemPrompt = @"You are a helpful assistant for Jan Enterprise, a fictional technology company.
You can answer general questions about the company's mission, values, and culture.
For questions about specific awards, winners, or categories, politely explain that internal documentation is needed.
Keep responses concise and friendly.";

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(userQuestion);

        var response = await _chatCompletion.GetChatMessageContentAsync(chatHistory);

        var answer = response.Content ?? "I'm not sure how to answer that.";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"💬 {answer}");
        Console.ResetColor();
        Console.WriteLine();

        var agentResponse = new AgentResponse(
            userQuestion,
            answer,
            "GeneralChat (local)"
        );

        await context.EmitEventAsync(new KernelProcessEvent
        {
            Id = ProcessEvents.DraftReady,
            Data = agentResponse
        });
    }

    private static void PrintHeader(string agentName)
    {
        Console.BackgroundColor = ConsoleColor.DarkGreen;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{agentName}]");
        Console.ResetColor();
        Console.WriteLine();
    }
}