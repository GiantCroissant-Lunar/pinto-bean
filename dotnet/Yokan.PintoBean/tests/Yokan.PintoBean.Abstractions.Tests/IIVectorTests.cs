using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for IIVector interface contract.
/// </summary>
public class IIVectorTests
{
    [Fact]
    public void IIVector_ShouldHaveCorrectMethods()
    {
        // Arrange
        var serviceType = typeof(IIVector);

        // Act
        var generateEmbeddingMethod = serviceType.GetMethod(nameof(IIVector.GenerateEmbeddingAsync));
        var generateEmbeddingsBatchMethod = serviceType.GetMethod(nameof(IIVector.GenerateEmbeddingsBatchAsync));
        var calculateSimilarityMethod = serviceType.GetMethod(nameof(IIVector.CalculateSimilarityAsync));
        var searchSimilarMethod = serviceType.GetMethod(nameof(IIVector.SearchSimilarAsync));
        var calculateDistanceMethod = serviceType.GetMethod(nameof(IIVector.CalculateDistanceAsync));

        // Assert
        Assert.NotNull(generateEmbeddingMethod);
        Assert.NotNull(generateEmbeddingsBatchMethod);
        Assert.NotNull(calculateSimilarityMethod);
        Assert.NotNull(searchSimilarMethod);
        Assert.NotNull(calculateDistanceMethod);

        // Verify method signatures
        Assert.Equal(typeof(Task<VectorResponse>), generateEmbeddingMethod.ReturnType);
        Assert.Equal(typeof(Task<IEnumerable<VectorResponse>>), generateEmbeddingsBatchMethod.ReturnType);
        Assert.Equal(typeof(Task<double>), calculateSimilarityMethod.ReturnType);
        Assert.Equal(typeof(Task<IEnumerable<VectorSearchResult>>), searchSimilarMethod.ReturnType);
        Assert.Equal(typeof(Task<double>), calculateDistanceMethod.ReturnType);
    }

    [Fact]
    public void IIVector_GenerateEmbeddingAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIVector);
        var method = serviceType.GetMethod(nameof(IIVector.GenerateEmbeddingAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(VectorRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIVector_GenerateEmbeddingsBatchAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIVector);
        var method = serviceType.GetMethod(nameof(IIVector.GenerateEmbeddingsBatchAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(IEnumerable<VectorRequest>), parameters[0].ParameterType);
        Assert.Equal("requests", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIVector_CalculateSimilarityAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIVector);
        var method = serviceType.GetMethod(nameof(IIVector.CalculateSimilarityAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(float[]), parameters[0].ParameterType);
        Assert.Equal("vector1", parameters[0].Name);
        Assert.Equal(typeof(float[]), parameters[1].ParameterType);
        Assert.Equal("vector2", parameters[1].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.Equal("cancellationToken", parameters[2].Name);
        Assert.True(parameters[2].HasDefaultValue);
    }

    [Fact]
    public void IIVector_SearchSimilarAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIVector);
        var method = serviceType.GetMethod(nameof(IIVector.SearchSimilarAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(VectorSearchRequest), parameters[0].ParameterType);
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void IIVector_CalculateDistanceAsync_ShouldHaveCorrectParameters()
    {
        // Arrange
        var serviceType = typeof(IIVector);
        var method = serviceType.GetMethod(nameof(IIVector.CalculateDistanceAsync));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(float[]), parameters[0].ParameterType);
        Assert.Equal("vector1", parameters[0].Name);
        Assert.Equal(typeof(float[]), parameters[1].ParameterType);
        Assert.Equal("vector2", parameters[1].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.Equal("cancellationToken", parameters[2].Name);
        Assert.True(parameters[2].HasDefaultValue);
    }

    [Fact]
    public void IIVector_ShouldBeInterface()
    {
        // Arrange
        var serviceType = typeof(IIVector);

        // Act & Assert
        Assert.True(serviceType.IsInterface);
        Assert.True(serviceType.IsPublic);
    }

    [Fact]
    public void IIVector_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var serviceType = typeof(IIVector);

        // Act & Assert
        Assert.Equal("Yokan.PintoBean.Abstractions", serviceType.Namespace);
    }

    [Fact]
    public void IIVector_ShouldFollowNamingConvention()
    {
        // Arrange
        var serviceType = typeof(IIVector);

        // Act & Assert
        Assert.Equal("IIVector", serviceType.Name);
        Assert.StartsWith("I", serviceType.Name);
    }
}