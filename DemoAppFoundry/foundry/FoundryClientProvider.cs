// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Microsoft.SemanticKernel.Agents.AzureAI;

namespace sk_azure_agent_demo.foundry;

/// <summary>
/// Provides Azure AI Foundry client instances with configuration.
/// Isolates Foundry-specific integration from orchestration logic.
/// </summary>
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
    string projectEndpoint,
    string connectionString,
    string researchAgentId,
    string? tenantId,
    string? clientId,
    string? clientSecret)
    {
        var credential = CreateCredential(tenantId, clientId, clientSecret);

        // IMPORTANT: pass endpoint as string
        var client = new AIProjectClient(projectEndpoint, credential);

        var clientProvider = AzureAIClientProvider.FromConnectionString(
            connectionString,
            credential);

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
                new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                });
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        });
    }
}
