using Microsoft.SemanticKernel;
using Azure.AI.Agents.Persistent;
using DemoAppFoundry.Foundry;

namespace DemoAppFoundry.Steps;

public sealed class ResearchAgent(FoundryClientProvider foundryProvider) : KernelProcessStep
{
    private readonly FoundryClientProvider _foundryProvider = foundryProvider;

    [KernelFunction]
    public async ValueTask<string> ExecuteAsync(KernelProcessStepContext context, string threadId)
    {
        Console.WriteLine();
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("[Researcher Agent]");
        Console.ResetColor();
        Console.WriteLine();

        try
        {
            var agentsClient = _foundryProvider.PersistentAgents;

            PersistentAgent agent = agentsClient.Administration.GetAgent(_foundryProvider.ResearchAgentId);

            ThreadRun run = agentsClient.Runs.CreateRun(threadId, agent.Id);

            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
            {
                await Task.Delay(500);
                run = agentsClient.Runs.GetRun(threadId, run.Id);
            }

            if (run.Status != RunStatus.Completed)
            {
                throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}");
            }

            var messages = agentsClient.Messages.GetMessages(threadId, order: ListSortOrder.Ascending);

            foreach (var msg in messages)
            {
                if (msg.Role != MessageRole.Agent) continue;

                foreach (var item in msg.ContentItems)
                {
                    if (item is MessageTextContent text && !string.IsNullOrWhiteSpace(text.Text))
                    {
                        Console.WriteLine(text.Text);
                    }
                }
            }

            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return threadId;
    }
}
