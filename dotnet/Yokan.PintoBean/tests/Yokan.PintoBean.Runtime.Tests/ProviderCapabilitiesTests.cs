using Xunit;
using Yokan.PintoBean.Runtime;
using System;
using System.Linq;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for ProviderCapabilities model and its functionality.
/// </summary>
public class ProviderCapabilitiesTests
{
    [Fact]
    public void Create_WithValidProviderId_ShouldCreateInstance()
    {
        // Act
        var capabilities = ProviderCapabilities.Create("test-provider");

        // Assert
        Assert.Equal("test-provider", capabilities.ProviderId);
        Assert.Equal(Platform.Any, capabilities.Platform);
        Assert.Equal(Priority.Normal, capabilities.Priority);
        Assert.Empty(capabilities.Tags);
        Assert.Empty(capabilities.Metadata);
        Assert.True(capabilities.RegisteredAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidProviderId_ShouldThrowArgumentException(string providerId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ProviderCapabilities.Create(providerId));
    }

    [Fact]
    public void WithPlatform_ShouldReturnNewInstanceWithPlatform()
    {
        // Arrange
        var original = ProviderCapabilities.Create("test-provider");

        // Act
        var updated = original.WithPlatform(Platform.Unity);

        // Assert
        Assert.Equal(Platform.Unity, updated.Platform);
        Assert.Equal(Platform.Any, original.Platform); // Original unchanged
        Assert.Equal(original.ProviderId, updated.ProviderId);
    }

    [Fact]
    public void WithPriority_ShouldReturnNewInstanceWithPriority()
    {
        // Arrange
        var original = ProviderCapabilities.Create("test-provider");

        // Act
        var updated = original.WithPriority(Priority.High);

        // Assert
        Assert.Equal(Priority.High, updated.Priority);
        Assert.Equal(Priority.Normal, original.Priority); // Original unchanged
        Assert.Equal(original.ProviderId, updated.ProviderId);
    }

    [Fact]
    public void WithTags_ShouldReturnNewInstanceWithTags()
    {
        // Arrange
        var original = ProviderCapabilities.Create("test-provider");
        var tags = new[] { "analytics", "primary" };

        // Act
        var updated = original.WithTags(tags);

        // Assert
        Assert.Equal(2, updated.Tags.Count);
        Assert.Contains("analytics", updated.Tags);
        Assert.Contains("primary", updated.Tags);
        Assert.Empty(original.Tags); // Original unchanged
    }

    [Fact]
    public void AddTags_ShouldReturnNewInstanceWithCombinedTags()
    {
        // Arrange
        var original = ProviderCapabilities.Create("test-provider")
            .WithTags("existing", "tag");

        // Act
        var updated = original.AddTags("new", "tag", "additional");

        // Assert
        Assert.Equal(4, updated.Tags.Count);
        Assert.Contains("existing", updated.Tags);
        Assert.Contains("tag", updated.Tags);
        Assert.Contains("new", updated.Tags);
        Assert.Contains("additional", updated.Tags);
    }

    [Fact]
    public void AddTags_WithNullOrEmpty_ShouldReturnSameInstance()
    {
        // Arrange
        var original = ProviderCapabilities.Create("test-provider");

        // Act
        var updated1 = original.AddTags();
        var updated2 = original.AddTags(null!);

        // Assert
        Assert.Same(original, updated1);
        Assert.Same(original, updated2);
    }

    [Fact]
    public void WithMetadata_ShouldReturnNewInstanceWithMetadata()
    {
        // Arrange
        var original = ProviderCapabilities.Create("test-provider");
        var metadata = new Dictionary<string, object>
        {
            { "version", "1.0.0" },
            { "author", "TestAuthor" }
        };

        // Act
        var updated = original.WithMetadata(metadata);

        // Assert
        Assert.Equal(2, updated.Metadata.Count);
        Assert.Equal("1.0.0", updated.Metadata["version"]);
        Assert.Equal("TestAuthor", updated.Metadata["author"]);
        Assert.Empty(original.Metadata); // Original unchanged
    }

    [Fact]
    public void AddMetadata_ShouldReturnNewInstanceWithAdditionalMetadata()
    {
        // Arrange
        var original = ProviderCapabilities.Create("test-provider")
            .AddMetadata("existing", "value");

        // Act
        var updated = original.AddMetadata("new", "newValue");

        // Assert
        Assert.Equal(2, updated.Metadata.Count);
        Assert.Equal("value", updated.Metadata["existing"]);
        Assert.Equal("newValue", updated.Metadata["new"]);
    }

    [Fact]
    public void AddMetadata_WithNullOrEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var capabilities = ProviderCapabilities.Create("test-provider");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => capabilities.AddMetadata("", "value"));
        Assert.Throws<ArgumentException>(() => capabilities.AddMetadata(null!, "value"));
    }

    [Fact]
    public void HasTags_WithAllRequiredTags_ShouldReturnTrue()
    {
        // Arrange
        var capabilities = ProviderCapabilities.Create("test-provider")
            .WithTags("analytics", "primary", "telemetry");

        // Act & Assert
        Assert.True(capabilities.HasTags("analytics"));
        Assert.True(capabilities.HasTags("analytics", "primary"));
        Assert.True(capabilities.HasTags("analytics", "primary", "telemetry"));
    }

    [Fact]
    public void HasTags_WithMissingRequiredTags_ShouldReturnFalse()
    {
        // Arrange
        var capabilities = ProviderCapabilities.Create("test-provider")
            .WithTags("analytics", "primary");

        // Act & Assert
        Assert.False(capabilities.HasTags("missing"));
        Assert.False(capabilities.HasTags("analytics", "missing"));
        Assert.False(capabilities.HasTags("telemetry"));
    }

    [Fact]
    public void HasTags_WithNullOrEmpty_ShouldReturnTrue()
    {
        // Arrange
        var capabilities = ProviderCapabilities.Create("test-provider");

        // Act & Assert
        Assert.True(capabilities.HasTags());
        Assert.True(capabilities.HasTags(null!));
    }

    [Fact]
    public void HasAnyTag_WithMatchingTags_ShouldReturnTrue()
    {
        // Arrange
        var capabilities = ProviderCapabilities.Create("test-provider")
            .WithTags("analytics", "primary");

        // Act & Assert
        Assert.True(capabilities.HasAnyTag("analytics"));
        Assert.True(capabilities.HasAnyTag("missing", "primary"));
        Assert.True(capabilities.HasAnyTag("analytics", "primary", "telemetry"));
    }

    [Fact]
    public void HasAnyTag_WithNoMatchingTags_ShouldReturnFalse()
    {
        // Arrange
        var capabilities = ProviderCapabilities.Create("test-provider")
            .WithTags("analytics", "primary");

        // Act & Assert
        Assert.False(capabilities.HasAnyTag("missing"));
        Assert.False(capabilities.HasAnyTag("missing", "notfound"));
        Assert.False(capabilities.HasAnyTag("telemetry"));
    }

    [Fact]
    public void HasAnyTag_WithNullOrEmpty_ShouldReturnFalse()
    {
        // Arrange
        var capabilities = ProviderCapabilities.Create("test-provider");

        // Act & Assert
        Assert.False(capabilities.HasAnyTag());
        Assert.False(capabilities.HasAnyTag(null!));
    }

    [Fact]
    public void FluentChaining_ShouldWorkCorrectly()
    {
        // Act
        var capabilities = ProviderCapabilities.Create("test-provider")
            .WithPlatform(Platform.Unity)
            .WithPriority(Priority.High)
            .WithTags("analytics", "primary")
            .AddTags("telemetry")
            .AddMetadata("version", "1.0.0")
            .AddMetadata("author", "TestAuthor");

        // Assert
        Assert.Equal("test-provider", capabilities.ProviderId);
        Assert.Equal(Platform.Unity, capabilities.Platform);
        Assert.Equal(Priority.High, capabilities.Priority);
        Assert.Equal(3, capabilities.Tags.Count);
        Assert.Contains("analytics", capabilities.Tags);
        Assert.Contains("primary", capabilities.Tags);
        Assert.Contains("telemetry", capabilities.Tags);
        Assert.Equal(2, capabilities.Metadata.Count);
        Assert.Equal("1.0.0", capabilities.Metadata["version"]);
        Assert.Equal("TestAuthor", capabilities.Metadata["author"]);
    }
}