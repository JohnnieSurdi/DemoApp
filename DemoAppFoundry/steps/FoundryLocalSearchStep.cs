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

            PA.PersistentAgent agent = agents.Administration.GetAgent(_foundryProvider.ResearchAgentId);

            PA.PersistentAgentThread thread = agents.Threads.CreateThread();

            agents.Messages.CreateMessage(thread.Id, PA.MessageRole.User, userQuestion);

            PA.ThreadRun run = agents.Runs.CreateRun(thread.Id, agent.Id);

            do
            {
                await Task.Delay(500);
                run = agents.Runs.GetRun(thread.Id, run.Id);
            }
            while (run.Status == PA.RunStatus.Queued || run.Status == PA.RunStatus.InProgress);

            if (run.Status != PA.RunStatus.Completed)
                throw new InvalidOperationException($"Run failed: {run.LastError?.Message}");

            var sb = new System.Text.StringBuilder();
            var messages = agents.Messages.GetMessages(thread.Id, order: PA.ListSortOrder.Ascending);

            foreach (var m in messages)
            {
                foreach (var item in m.ContentItems)
                {
                    if (item is PA.MessageTextContent text)
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
