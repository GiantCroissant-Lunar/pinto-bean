using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the provider lifecycle contracts (IQuiesceable, IProviderStateExport, IProviderStateImport).
/// These tests validate the contract definitions and basic integration scenarios.
/// </summary>
public class ProviderLifecycleContractsTests
{
    #region IQuiesceable Tests

    [Fact]
    public async Task IQuiesceable_QuiesceAsync_CanBeImplemented()
    {
        // Arrange
        var provider = new TestQuiesceableProvider();
        
        // Act
        await provider.QuiesceAsync();
        
        // Assert
        Assert.True(provider.WasQuiesced);
    }

    [Fact]
    public async Task IQuiesceable_QuiesceAsync_WithCancellation_RespectsToken()
    {
        // Arrange
        var provider = new TestQuiesceableProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => provider.QuiesceAsync(cts.Token));
    }

    #endregion

    #region IProviderStateExport Tests

    [Fact]
    public async Task IProviderStateExport_ExportStateAsync_CanBeImplemented()
    {
        // Arrange
        var provider = new TestStateExportProvider();
        
        // Act
        var state = await provider.ExportStateAsync();
        
        // Assert
        Assert.NotNull(state);
        Assert.Equal("test-data", state.Data);
        Assert.Equal("1.0", state.Version);
    }

    [Fact]
    public async Task IProviderStateExport_ExportStateAsync_WithCancellation_RespectsToken()
    {
        // Arrange
        var provider = new TestStateExportProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => provider.ExportStateAsync(cts.Token).AsTask());
    }

    #endregion

    #region IProviderStateImport Tests

    [Fact]
    public async Task IProviderStateImport_ImportStateAsync_CanBeImplemented()
    {
        // Arrange
        var provider = new TestStateImportProvider();
        var state = new ProviderState("imported-data", "1.0", DateTimeOffset.UtcNow);
        
        // Act
        await provider.ImportStateAsync(state);
        
        // Assert
        Assert.Equal("imported-data", provider.ImportedData);
    }

    [Fact]
    public async Task IProviderStateImport_ImportStateAsync_WithNullState_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new TestStateImportProvider();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.ImportStateAsync(null!));
    }

    [Fact]
    public async Task IProviderStateImport_ImportStateAsync_WithCancellation_RespectsToken()
    {
        // Arrange
        var provider = new TestStateImportProvider();
        var state = new ProviderState("test-data", "1.0", DateTimeOffset.UtcNow);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => provider.ImportStateAsync(state, cts.Token));
    }

    #endregion

    #region ProviderState Tests

    [Fact]
    public void ProviderState_Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var data = "test-data";
        var version = "1.0";
        var timestamp = DateTimeOffset.UtcNow;
        
        // Act
        var state = new ProviderState(data, version, timestamp);
        
        // Assert
        Assert.Equal(data, state.Data);
        Assert.Equal(version, state.Version);
        Assert.Equal(timestamp, state.Timestamp);
    }

    [Fact]
    public void ProviderState_Constructor_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderState(null!, "1.0", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ProviderState_Constructor_WithNullVersion_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProviderState("data", null!, DateTimeOffset.UtcNow));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteProviderLifecycle_QuiesceExportImport_WorksTogether()
    {
        // Arrange
        var sourceProvider = new TestCompleteLifecycleProvider();
        var targetProvider = new TestCompleteLifecycleProvider();
        
        // Act - Quiesce the source provider
        await sourceProvider.QuiesceAsync();
        
        // Act - Export state from source
        var state = await sourceProvider.ExportStateAsync();
        
        // Act - Import state to target
        await targetProvider.ImportStateAsync(state);
        
        // Assert
        Assert.True(sourceProvider.WasQuiesced);
        Assert.Equal(sourceProvider.TestData, targetProvider.ImportedData);
    }

    #endregion

    #region Test Implementation Classes

    private class TestQuiesceableProvider : IQuiesceable
    {
        public bool WasQuiesced { get; private set; }

        public Task QuiesceAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasQuiesced = true;
            return Task.CompletedTask;
        }
    }

    private class TestStateExportProvider : IProviderStateExport
    {
        public ValueTask<ProviderState> ExportStateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = new ProviderState("test-data", "1.0", DateTimeOffset.UtcNow);
            return ValueTask.FromResult(state);
        }
    }

    private class TestStateImportProvider : IProviderStateImport
    {
        public object? ImportedData { get; private set; }

        public Task ImportStateAsync(ProviderState state, CancellationToken cancellationToken = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            cancellationToken.ThrowIfCancellationRequested();
            
            ImportedData = state.Data;
            return Task.CompletedTask;
        }
    }

    private class TestCompleteLifecycleProvider : IQuiesceable, IProviderStateExport, IProviderStateImport
    {
        public bool WasQuiesced { get; private set; }
        public string TestData { get; } = "lifecycle-test-data";
        public object? ImportedData { get; private set; }

        public Task QuiesceAsync(CancellationToken cancellationToken = default)
        {
            WasQuiesced = true;
            return Task.CompletedTask;
        }

        public ValueTask<ProviderState> ExportStateAsync(CancellationToken cancellationToken = default)
        {
            var state = new ProviderState(TestData, "1.0", DateTimeOffset.UtcNow);
            return ValueTask.FromResult(state);
        }

        public Task ImportStateAsync(ProviderState state, CancellationToken cancellationToken = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            ImportedData = state.Data;
            return Task.CompletedTask;
        }
    }

    #endregion
}