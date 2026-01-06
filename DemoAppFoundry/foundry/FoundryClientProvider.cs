// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents.AzureAI;

namespace sk_azure_agent_demo.foundry;

public sealed class FoundryClientProvider
{
    public AIProjectClient Client { get; }
    public string ResearchAgentId { get; }

    private readonly AzureAIClientProvider _clientProvider;

    private FoundryClientProvider(AIProjectClient client, AzureAIClientProvider clientProvider, string researchAgentId)
    {
        Client = client;
        _clientProvider = clientProvider;
        ResearchAgentId = researchAgentId;
    }

    public static FoundryClientProvider Create(
        string connectionString,      // ✅ required
        string researchAgentId,
        string? tenantId,
        string? clientId,
        string? clientSecret)
    {
        var credential = CreateCredential(tenantId, clientId, clientSecret);
        Console.WriteLine("[DEBUG] Foundry connection string RAW: " + connectionString);

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            throw new ArgumentException(
                "Invalid connection string format. Expected: <endpoint>;<subscription_id>;<resource_group>;<project_name>",
                nameof(connectionString));

        var endpoint = parts[0].Trim();          // https://...services.ai.azure.com
        var subscriptionId = parts[1].Trim();
        var resourceGroup = parts[2].Trim();
        var projectName = parts[3].Trim();

        Console.WriteLine($"[DEBUG] Using structured AIProjectClient ctor:");
        Console.WriteLine($"[DEBUG] endpoint={endpoint}");
        Console.WriteLine($"[DEBUG] sub={subscriptionId}");
        Console.WriteLine($"[DEBUG] rg={resourceGroup}");
        Console.WriteLine($"[DEBUG] project={projectName}");
        // ✅ IMPORTANT: this ctor expects "<endpoint>;<sub>;<rg>;<project>"
        var client = new AIProjectClient(
            new Uri(endpoint),
            subscriptionId,
            resourceGroup,
            projectName,
            credential);

        var clientProvider = AzureAIClientProvider.FromConnectionString(connectionString, credential);

        return new FoundryClientProvider(client, clientProvider, researchAgentId);
    }

    public AzureAIClientProvider GetClientProvider() => _clientProvider;

    private static TokenCredential CreateCredential(string? tenantId, string? clientId, string? clientSecret)
    {
        if (!string.IsNullOrWhiteSpace(tenantId) &&
            !string.IsNullOrWhiteSpace(clientId) &&
            !string.IsNullOrWhiteSpace(clientSecret))
        {
            return new ClientSecretCredential(
                tenantId, clientId, clientSecret,
                new ClientSecretCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud });
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        });
    }
}
