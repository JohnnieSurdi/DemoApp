using Microsoft.SemanticKernel;
using DemoAppFoundry.Events;

namespace DemoAppFoundry.Steps;

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