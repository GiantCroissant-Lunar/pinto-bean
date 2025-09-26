using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Interface for discovering plugins from various sources.
/// </summary>
public interface IPluginDiscovery
{
    /// <summary>
    /// Discovers plugins from the specified directory by scanning for manifest files.
    /// </summary>
    /// <param name="directory">The directory to search for plugin manifests.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An array of discovered plugin descriptors.</returns>
    Task<PluginDescriptor[]> DiscoverPluginsAsync(string directory, CancellationToken cancellationToken = default);
}