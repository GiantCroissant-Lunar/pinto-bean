using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for HelloResponse DTO.
/// </summary>
public class HelloResponseTests
{
    [Fact]
    public void HelloResponse_WithRequiredMessage_ShouldCreate()
    {
        // Arrange & Act
        var response = new HelloResponse { Message = "Hello, World!" };

        // Assert
        Assert.Equal("Hello, World!", response.Message);
        Assert.Equal("en", response.Language);
        Assert.Null(response.ServiceInfo);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.True(response.Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void HelloResponse_WithAllProperties_ShouldCreate()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var response = new HelloResponse 
        { 
            Message = "Bonjour, Alice!",
            Language = "fr",
            ServiceInfo = "TestService v1.0",
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal("Bonjour, Alice!", response.Message);
        Assert.Equal("fr", response.Language);
        Assert.Equal("TestService v1.0", response.ServiceInfo);
        Assert.Equal(timestamp, response.Timestamp);
    }

    [Fact]
    public void HelloResponse_RecordEquality_ShouldWork()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var response1 = new HelloResponse 
        { 
            Message = "Hello!", 
            Language = "en",
            Timestamp = timestamp
        };
        var response2 = new HelloResponse 
        { 
            Message = "Hello!", 
            Language = "en",
            Timestamp = timestamp
        };
        var response3 = new HelloResponse 
        { 
            Message = "Hola!", 
            Language = "es",
            Timestamp = timestamp
        };

        // Act & Assert
        Assert.Equal(response1, response2);
        Assert.NotEqual(response1, response3);
    }
}