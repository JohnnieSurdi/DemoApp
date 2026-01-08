namespace DemoAppFoundry.Models;

public sealed record AgentResponse(
    string UserQuestion,
    string DraftAnswer,
    string Provenance
);