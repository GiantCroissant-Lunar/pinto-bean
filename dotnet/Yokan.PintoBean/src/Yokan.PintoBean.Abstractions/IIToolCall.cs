// Tier-1: AI tool calling service contract for Yokan PintoBean service platform

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Tier-1 AI tool calling service contract.
/// Engine-free interface following the 4-tier architecture pattern.
/// Provides AI-driven tool selection and execution coordination.
/// </summary>
public interface IIToolCall
{
    /// <summary>
    /// Analyzes a query and determines which tools (if any) should be called to fulfill the request.
    /// The AI evaluates the available tools and decides whether to use them based on the query context.
    /// </summary>
    /// <param name="request">The tool call request containing the query and available tools.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tool call response with the AI's decision and any tool calls to make.</returns>
    Task<ToolCallResponse> PlanToolCallsAsync(ToolCallRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a specific tool call and returns the result.
    /// This method handles the actual execution of individual tools identified by the AI.
    /// </summary>
    /// <param name="toolCall">The tool call to execute with its parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<ToolCallResult> ExecuteToolCallAsync(ToolCall toolCall, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple tool calls in parallel and returns their results.
    /// More efficient than individual execution when multiple tools need to be called.
    /// </summary>
    /// <param name="toolCalls">The collection of tool calls to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of tool call results corresponding to the input tool calls.</returns>
    Task<IEnumerable<ToolCallResult>> ExecuteToolCallsBatchAsync(IEnumerable<ToolCall> toolCalls, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes tool execution results and generates a final response based on the outcomes.
    /// The AI synthesizes the tool results into a coherent response for the original query.
    /// </summary>
    /// <param name="originalRequest">The original tool call request.</param>
    /// <param name="toolResults">The results from executing the planned tools.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A final response incorporating the tool execution results.</returns>
    Task<ToolCallResponse> SynthesizeResponseAsync(ToolCallRequest originalRequest, IEnumerable<ToolCallResult> toolResults, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a tool definition is properly structured and usable.
    /// Checks the tool's schema, parameters, and other requirements for AI compatibility.
    /// </summary>
    /// <param name="tool">The tool definition to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the tool definition is valid, false otherwise.</returns>
    Task<bool> ValidateToolDefinitionAsync(ToolDefinition tool, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new tool that the AI can use in future tool calling operations.
    /// Expands the AI's capabilities by adding new tools to its toolkit.
    /// </summary>
    /// <param name="tool">The tool definition to register.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the tool was successfully registered, false otherwise.</returns>
    Task<bool> RegisterToolAsync(ToolDefinition tool, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all currently registered tools available for AI tool calling.
    /// Useful for debugging, administration, and understanding the AI's capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of all registered tool definitions.</returns>
    Task<IEnumerable<ToolDefinition>> GetRegisteredToolsAsync(CancellationToken cancellationToken = default);
}