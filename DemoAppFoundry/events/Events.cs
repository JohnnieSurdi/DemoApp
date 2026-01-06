// Copyright (c) Microsoft. All rights reserved.
namespace Events;

/// <summary>
/// Processes Events emitted by shared steps.<br/>
/// </summary>
public static class ProcessEvents
{
    public static readonly string StartProcess = nameof(StartProcess);
    public static readonly string Start = nameof(Start);
    public static readonly string RoutedToGeneral = nameof(RoutedToGeneral);
    public static readonly string RoutedToDocs = nameof(RoutedToDocs);
    public static readonly string DraftReady = nameof(DraftReady);
    public static readonly string NeedsEdit = nameof(NeedsEdit);
    public static readonly string Approved = nameof(Approved);
    public static readonly string Completed = nameof(Completed);
    public static readonly string NeedMoreData = nameof(NeedMoreData);
}