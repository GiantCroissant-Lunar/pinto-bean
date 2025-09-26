using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Providers.SamplePlugin;

/// <summary>
/// Sample plugin implementation of IHelloService with enhanced behavior.
/// This is version 2.0 of the sample plugin with improved messages.
/// </summary>
public class SampleHelloProviderV2 : IHelloService
{
    /// <summary>
    /// The version of this enhanced sample plugin provider.
    /// </summary>
    public const string Version = "2.0.0";
    
    /// <summary>
    /// The provider identifier for this enhanced sample plugin.
    /// </summary>
    public const string ProviderId = "sample-hello-v2";

    /// <summary>
    /// Returns an enhanced greeting message for the specified name using the sample plugin v2.
    /// </summary>
    /// <param name="request">The hello request containing the name and optional parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A hello response with the enhanced greeting message from the sample plugin.</returns>
    public Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"🚀 Enhanced Plugin Hello, {request.Name}! Welcome to Sample Plugin v{Version} with improved features!",
            ServiceInfo = $"SampleHelloProviderV2 v{Version}",
            Language = request.Language ?? "en"
        });
    }

    /// <summary>
    /// Returns an enhanced farewell message for the specified name using the sample plugin v2.
    /// </summary>
    /// <param name="request">The goodbye request containing the name and optional parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A hello response with the enhanced farewell message from the sample plugin.</returns>
    public Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HelloResponse
        {
            Message = $"🚀 Enhanced Plugin Farewell, {request.Name}! Hope you enjoyed the upgraded Sample Plugin v{Version}!",
            ServiceInfo = $"SampleHelloProviderV2 v{Version}",
            Language = request.Language ?? "en"
        });
    }
}