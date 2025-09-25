using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for AlcLoadContext implementation.
/// </summary>
public class AlcLoadContextTests
{
    [Fact]
    public void AlcLoadContext_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        using var context = new AlcLoadContext("test-context");

        // Assert
        Assert.Equal("test-context", context.Id);
        Assert.False(context.IsDisposed);
    }

    [Fact]
    public void AlcLoadContext_Constructor_WithNullId_GeneratesId()
    {
        // Arrange & Act
        using var context = new AlcLoadContext();

        // Assert
        Assert.NotNull(context.Id);
        Assert.NotEmpty(context.Id);
        Assert.False(context.IsDisposed);
    }

    [Fact]
    [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", 
        Justification = "Test code handles single-file scenario appropriately.")]
    public void Load_WithValidAssemblyPath_LoadsAssembly()
    {
        // Arrange
        using var context = new AlcLoadContext();
        var assemblyPath = typeof(AlcLoadContextTests).Assembly.Location;
        // Skip test if running in single-file mode where Location is empty
        if (string.IsNullOrEmpty(assemblyPath))
        {
            assemblyPath = AppContext.BaseDirectory + "Yokan.PintoBean.Runtime.Tests.dll";
            if (!System.IO.File.Exists(assemblyPath))
            {
                // Skip this test in single-file scenarios
                return;
            }
        }

        // Act
        var assembly = context.Load(assemblyPath);

        // Assert
        Assert.NotNull(assembly);
    }

    [Fact]
    public void Load_WithNullOrEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        using var context = new AlcLoadContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Load(""));
        Assert.Throws<ArgumentException>(() => context.Load((string)null!));
        Assert.Throws<ArgumentException>(() => context.Load("   "));
    }

    [Fact]
    public void Load_WithNonExistentPath_ThrowsFileNotFoundException()
    {
        // Arrange
        using var context = new AlcLoadContext();
        var nonExistentPath = "/non/existent/path.dll";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => context.Load(nonExistentPath));
    }

    [Fact]
    public void Load_WithType_LoadsAssemblyContainingType()
    {
        // Arrange
        using var context = new AlcLoadContext();
        var expectedAssembly = typeof(string).Assembly;

        // Act
        var assembly = context.Load(typeof(string));

        // Assert
        Assert.Equal(expectedAssembly, assembly);
    }

    [Fact]
    public void Load_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = new AlcLoadContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.Load((Type)null!));
    }

    [Fact]
    public void TryGetType_WithValidTypeName_ReturnsTrue()
    {
        // Arrange
        using var context = new AlcLoadContext();
        var targetType = typeof(AlcLoadContextTests);
        var assembly = context.Load(targetType);
        var typeName = targetType.FullName!;

        // Act
        var found = context.TryGetType(typeName, out var type);

        // Assert
        Assert.True(found);
        Assert.Equal(targetType, type);
    }

    [Fact]
    public void TryGetType_WithInvalidTypeName_ReturnsFalse()
    {
        // Arrange
        using var context = new AlcLoadContext();
        var targetType = typeof(AlcLoadContextTests);
        context.Load(targetType);

        // Act
        var found = context.TryGetType("NonExistent.Type", out var type);

        // Assert
        Assert.False(found);
        Assert.Null(type);
    }

    [Fact]
    public void TryGetType_WithNullOrEmptyTypeName_ReturnsFalse()
    {
        // Arrange
        using var context = new AlcLoadContext();

        // Act & Assert
        Assert.False(context.TryGetType(null!, out _));
        Assert.False(context.TryGetType("", out _));
        Assert.False(context.TryGetType("   ", out _));
    }

    [Fact]
    public void CreateInstance_WithValidType_CreatesInstance()
    {
        // Arrange
        using var context = new AlcLoadContext();

        // Act
        var instance = context.CreateInstance<object>();

        // Assert
        Assert.NotNull(instance);
        Assert.IsType<object>(instance);
    }

    [Fact]
    public void CreateInstance_WithTypeAndArguments_CreatesInstance()
    {
        // Arrange
        using var context = new AlcLoadContext();

        // Act
        var instance = context.CreateInstance(typeof(TestClassWithConstructor), "test-value");

        // Assert
        Assert.NotNull(instance);
        Assert.IsType<TestClassWithConstructor>(instance);
        Assert.Equal("test-value", ((TestClassWithConstructor)instance).Value);
    }

    [Fact]
    public void CreateInstance_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = new AlcLoadContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.CreateInstance((Type)null!));
    }

    [Fact]
    public void CreateInstance_WithAbstractType_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new AlcLoadContext();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.CreateInstance(typeof(AbstractTestClass)));
    }

    [Fact]
    public void Dispose_MarksContextAsDisposed()
    {
        // Arrange
        var context = new AlcLoadContext();
        Assert.False(context.IsDisposed);

        // Act
        context.Dispose();

        // Assert
        Assert.True(context.IsDisposed);
    }

    [Fact]
    public void Dispose_MultipleCallsDoNotThrow()
    {
        // Arrange
        var context = new AlcLoadContext();

        // Act & Assert
        context.Dispose();
        context.Dispose(); // Should not throw
    }

    [Fact]
    public void Methods_AfterDispose_ThrowInvalidOperationException()
    {
        // Arrange
        var context = new AlcLoadContext();
        context.Dispose();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.Load("/some/path.dll"));
        Assert.Throws<InvalidOperationException>(() => context.Load(typeof(string)));
        Assert.Throws<InvalidOperationException>(() => context.TryGetType("SomeType", out _));
        Assert.Throws<InvalidOperationException>(() => context.CreateInstance<object>());
        Assert.Throws<InvalidOperationException>(() => context.CreateInstance(typeof(object)));
    }

    // Test helper classes
    public class TestClassWithConstructor
    {
        public TestClassWithConstructor(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    public abstract class AbstractTestClass
    {
        public abstract string GetMessage();
    }
}