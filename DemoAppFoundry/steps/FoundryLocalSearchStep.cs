using Microsoft.SemanticKernel;
using DemoAppFoundry.Events;
using DemoAppFoundry.Models;
using PA = Azure.AI.Agents.Persistent;
using DemoAppFoundry.Foundry;

namespace DemoAppFoundry.Steps;

public sealed class FoundryLocalSearchStep(FoundryClientProvider foundryProvider) : KernelProcessStep
{
    private readonly FoundryClientProvider _foundryProvider = foundryProvider;

    [KernelFunction]
    public async ValueTask ExecuteAsync(KernelProcessStepContext context, string userQuestion)
    {
        PrintHeader("Foundry Local Search Agent");

        try
        {
            var agents = _foundryProvider.PersistentAgents;

            PA.PersistentAgent agent =
                await agents.Administration.GetAgentAsync(_foundryProvider.ResearchAgentId);

            PA.PersistentAgentThread thread =
                await agents.Threads.CreateThreadAsync();

            await agents.Messages.CreateMessageAsync(thread.Id, PA.MessageRole.User, userQuestion);

            PA.ThreadRun run =
                await agents.Runs.CreateRunAsync(thread.Id, agent.Id);

            while (run.Status == PA.RunStatus.Queued || run.Status == PA.RunStatus.InProgress)
            {
                await Task.Delay(500);
                run = await agents.Runs.GetRunAsync(thread.Id, run.Id);
            }

            if (run.Status != PA.RunStatus.Completed)
                throw new InvalidOperationException($"Run failed: {run.LastError?.Message}");

            var sb = new System.Text.StringBuilder();

            await foreach (var m in agents.Messages.GetMessagesAsync(thread.Id, order: PA.ListSortOrder.Ascending))
            {
                foreach (var item in m.ContentItems)
                {
                    if (item is PA.MessageTextContent text && !string.IsNullOrWhiteSpace(text.Text))
                        sb.AppendLine(text.Text);
                }
            }

            var answer = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(answer))
                answer = "(No text output returned)";

            var agentResponse = new AgentResponse(userQuestion, answer, "Foundry Persistent Agent");

            await context.EmitEventAsync(new KernelProcessEvent
            {
                Id = ProcessEvents.DraftReady,
                Data = agentResponse
            });
        }
        catch (Exception ex)
        {
            var errorResponse = new AgentResponse(userQuestion, $"Error: {ex.Message}", "FoundryLocalSearch (Error)");
            await context.EmitEventAsync(new KernelProcessEvent { Id = ProcessEvents.DraftReady, Data = errorResponse });
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
