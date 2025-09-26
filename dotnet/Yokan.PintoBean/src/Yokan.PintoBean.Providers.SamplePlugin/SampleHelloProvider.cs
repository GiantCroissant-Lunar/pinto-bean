using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Providers.SamplePlugin;

/// <summary>
/// Sample plugin implementation of IHelloService with distinct behavior.
/// This is version 1.0 of the sample plugin.
/// </summary>
public class SampleHelloProvider : IHelloService
{
    /// <summary>
    /// The version of this sample plugin provider.
    /// </summary>
    public const string Version = "1.0.0";
    
    /// <summary>
    /// The provider identifier for this sample plugin.
    /// </summary>
    public const string ProviderId = "sample-hello-v1";

    /// <summary>
    /// Returns a greeting message for the specified name using the sample plugin v1.
    /// </summary>
    /// <param name="request">The hello request containing the name and optional parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A hello response with the greeting message from the sample plugin.</returns>
    public Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"🔌 Plugin Hello, {request.Name}! This is Sample Plugin v{Version}.",
            ServiceInfo = $"SampleHelloProvider v{Version}",
            Language = request.Language ?? "en"
        });
    }

    /// <summary>
    /// Returns a farewell message for the specified name using the sample plugin v1.
    /// </summary>
    /// <param name="request">The goodbye request containing the name and optional parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A hello response with the farewell message from the sample plugin.</returns>
    public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"🔌 Plugin Goodbye, {request.Name}! Thanks for trying Sample Plugin v{Version}.",
            ServiceInfo = $"SampleHelloProvider v{Version}",
            Language = request.Language ?? "en"
        });
    }
}