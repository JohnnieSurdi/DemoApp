using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;

public sealed class FoundryClientProvider
{
    public AIProjectClient ProjectClient { get; }
    public PersistentAgentsClient PersistentAgents { get; }
    public string ResearchAgentId { get; }

    private FoundryClientProvider(AIProjectClient projectClient, PersistentAgentsClient persistentAgents, string researchAgentId)
    {
        ProjectClient = projectClient;
        PersistentAgents = persistentAgents;
        ResearchAgentId = researchAgentId;
    }

    public static FoundryClientProvider Create(string projectEndpoint, string researchAgentId, TokenCredential credential)
    {
        var projectClient = new AIProjectClient(new Uri(projectEndpoint), credential);

        // This exists once you’re on a newer Azure.AI.Projects version:
        var persistent = projectClient.GetPersistentAgentsClient();

        return new FoundryClientProvider(projectClient, persistent, researchAgentId);
    }
}
