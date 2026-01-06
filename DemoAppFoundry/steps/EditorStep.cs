// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Models;

namespace Steps;

/// <summary>
/// Final step that formats and displays the agent response.
/// </summary>
public sealed class EditorStep : KernelProcessStep
{
    [KernelFunction]
    public async ValueTask ExecuteAsync(KernelProcessStepContext context, AgentResponse response)
    {
        PrintHeader("Editor");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("📋 FINAL RESPONSE");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"Question: {response.UserQuestion}");
        Console.WriteLine();
        Console.WriteLine($"Answer: {response.DraftAnswer}");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Source: {response.Provenance}");
        Console.ResetColor();
        Console.WriteLine();

        await Task.CompletedTask;
    }

    private static void PrintHeader(string agentName)
    {
        Console.BackgroundColor = ConsoleColor.DarkYellow;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write($"[{agentName}]");
        Console.ResetColor();
        Console.WriteLine();
    }
}