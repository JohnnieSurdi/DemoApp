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

            PersistentAgent agent =
                await agentsClient.Administration.GetAgentAsync(_foundryProvider.ResearchAgentId);

            ThreadRun run =
                await agentsClient.Runs.CreateRunAsync(threadId, agent.Id);

            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
            {
                await Task.Delay(500);
                run = await agentsClient.Runs.GetRunAsync(threadId, run.Id);
            }

            if (run.Status != RunStatus.Completed)
                throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}");

            await foreach (var msg in agentsClient.Messages.GetMessagesAsync(threadId, order: ListSortOrder.Ascending))
            {
                if (msg.Role != MessageRole.Agent) continue;

                foreach (var item in msg.ContentItems)
                {
                    if (item is MessageTextContent text && !string.IsNullOrWhiteSpace(text.Text))
                        Console.WriteLine(text.Text);
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
