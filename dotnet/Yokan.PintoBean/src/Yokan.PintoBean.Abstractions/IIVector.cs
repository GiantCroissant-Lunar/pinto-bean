// Tier-1: AI vector operations service contract for Yokan PintoBean service platform

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Tier-1 AI vector operations service contract.
/// Engine-free interface following the 4-tier architecture pattern.
/// Provides vector embedding generation and similarity operations.
/// </summary>
public interface IIVector
{
    /// <summary>
    /// Generates vector embeddings for the provided input text.
    /// Converts text into numerical vector representations for AI operations.
    /// </summary>
    /// <param name="request">The vector generation request containing input text and parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A vector response containing the generated embedding.</returns>
    Task<VectorResponse> GenerateEmbeddingAsync(VectorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates vector embeddings for multiple input texts in a single batch operation.
    /// More efficient than individual calls when processing multiple texts.
    /// </summary>
    /// <param name="requests">The collection of vector generation requests.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of vector responses corresponding to the input requests.</returns>
    Task<IEnumerable<VectorResponse>> GenerateEmbeddingsBatchAsync(IEnumerable<VectorRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates similarity between two vectors using cosine similarity.
    /// Returns a similarity score between 0.0 (completely dissimilar) and 1.0 (identical).
    /// </summary>
    /// <param name="vector1">The first vector for similarity comparison.</param>
    /// <param name="vector2">The second vector for similarity comparison.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A similarity score between 0.0 and 1.0.</returns>
    Task<double> CalculateSimilarityAsync(float[] vector1, float[] vector2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs vector similarity search to find the most similar vectors to a query vector.
    /// Useful for semantic search, recommendation systems, and similarity matching.
    /// </summary>
    /// <param name="request">The vector search request containing the query vector and search parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of vector search results ordered by similarity score (highest first).</returns>
    Task<IEnumerable<VectorSearchResult>> SearchSimilarAsync(VectorSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the Euclidean distance between two vectors.
    /// Alternative distance metric to cosine similarity, useful for different use cases.
    /// </summary>
    /// <param name="vector1">The first vector for distance calculation.</param>
    /// <param name="vector2">The second vector for distance calculation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The Euclidean distance between the vectors (lower values indicate higher similarity).</returns>
    Task<double> CalculateDistanceAsync(float[] vector1, float[] vector2, CancellationToken cancellationToken = default);
}