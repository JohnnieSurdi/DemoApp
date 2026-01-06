// Copyright (c) Microsoft. All rights reserved.
namespace Models;

/// <summary>
/// Data transfer object for agent responses.
/// </summary>
public sealed record AgentResponse(
    string UserQuestion,
    string DraftAnswer,
    string Provenance
);