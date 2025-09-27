// Enum for selecting aspect runtime types in Unity profiles

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Represents the different types of aspect runtime implementations available.
/// Used in Unity profile assets to configure which aspect runtime to use.
/// </summary>
public enum AspectRuntimeType
{
    /// <summary>
    /// No-operation aspect runtime that performs no telemetry collection.
    /// Minimal overhead, suitable for performance-critical scenarios.
    /// </summary>
    NoOp,

    /// <summary>
    /// Unity-specific aspect runtime that logs to UnityEngine.Debug.* with low-cardinality tags.
    /// Designed for Unity play mode with Unity-friendly log output.
    /// </summary>
    Unity,

    /// <summary>
    /// OpenTelemetry-backed aspect runtime for tracing and metrics collection.
    /// Maps aspect runtime operations to OpenTelemetry ActivitySource and Meter primitives.
    /// Available when running in .NET host with OpenTelemetry packages.
    /// </summary>
    OpenTelemetry,

    /// <summary>
    /// Adaptive aspect runtime that automatically detects Unity context and switches
    /// between Unity Debug logging and OpenTelemetry based on runtime environment.
    /// In Unity play mode, uses UnityAspectRuntime. In Editor with OpenTelemetry packages, can use OtelAspectRuntime.
    /// </summary>
    Adaptive
}