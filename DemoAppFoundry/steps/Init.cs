// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Events;

namespace Steps;

/// <summary>
/// Initialization step that receives user input and starts the process.
/// </summary>
public sealed class Init : KernelProcessStep
{
    [KernelFunction]
    public async ValueTask ExecuteAsync(KernelProcessStepContext context, string userInput)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Hybrid Multi-Agent System - Jan Enterprise Assistant      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($" Question: {userInput}");
        Console.WriteLine();

        await context.EmitEventAsync(new KernelProcessEvent
        {
            Id = ProcessEvents.Start,
            Data = userInput
        });
    }
}