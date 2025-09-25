using System;
using System.Reflection;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for FakeLoadContext implementation.
/// </summary>
public class FakeLoadContextTests
{
    [Fact]
    public void FakeLoadContext_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        using var context = new FakeLoadContext("test-context");

        // Assert
        Assert.Equal("test-context", context.Id);
        Assert.False(context.IsDisposed);
    }

    [Fact]
    public void FakeLoadContext_Constructor_WithNullId_GeneratesId()
    {
        // Arrange & Act
        using var context = new FakeLoadContext();

        // Assert
        Assert.NotNull(context.Id);
        Assert.NotEmpty(context.Id);
        Assert.False(context.IsDisposed);
    }

    [Fact]
    public void RegisterAssembly_WithValidAssembly_RegistersSuccessfully()
    {
        // Arrange
        using var context = new FakeLoadContext();
        var assembly = typeof(FakeLoadContextTests).Assembly;
        const string path = "/fake/test.dll";

        // Act
        context.RegisterAssembly(path, assembly);
        var loadedAssembly = context.Load(path);

        // Assert
        Assert.Equal(assembly, loadedAssembly);
    }

    [Fact]
    public void Load_WithUnregisteredPath_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new FakeLoadContext();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => context.Load("/nonexistent/path.dll"));
        Assert.Contains("Assembly not found", exception.Message);
        Assert.Contains("Use RegisterAssembly", exception.Message);
    }

    [Fact]
    public void Load_WithNullOrEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        using var context = new FakeLoadContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Load((string)null!));
        Assert.Throws<ArgumentException>(() => context.Load(""));
        Assert.Throws<ArgumentException>(() => context.Load("   "));
    }

    [Fact]
    public void Load_WithType_LoadsAssemblyContainingType()
    {
        // Arrange
        using var context = new FakeLoadContext();
        var type = typeof(FakeLoadContextTests);

        // Act
        var assembly = context.Load(type);

        // Assert
        Assert.Equal(type.Assembly, assembly);
    }

    [Fact]
    public void Load_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = new FakeLoadContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.Load((Type)null!));
    }

    [Fact]
    public void RegisterType_WithValidType_RegistersSuccessfully()
    {
        // Arrange
        using var context = new FakeLoadContext();
        var type = typeof(string);
        const string typeName = "System.String";

        // Act
        context.RegisterType(typeName, type);
        var found = context.TryGetType(typeName, out var foundType);

        // Assert
        Assert.True(found);
        Assert.Equal(type, foundType);
    }

    [Fact]
    public void TryGetType_WithUnregisteredType_ReturnsFalse()
    {
        // Arrange
        using var context = new FakeLoadContext();

        // Act
        var found = context.TryGetType("NonExistent.Type", out var type);

        // Assert
        Assert.False(found);
        Assert.Null(type);
    }

    [Fact]
    public void TryGetType_WithRegisteredAssembly_FindsTypesFromAssembly()
    {
        // Arrange
        using var context = new FakeLoadContext();
        var assembly = typeof(FakeLoadContextTests).Assembly;
        context.RegisterAssembly("/fake/test.dll", assembly);

        // Act
        var found = context.TryGetType(typeof(FakeLoadContextTests).FullName!, out var type);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(FakeLoadContextTests), type);
    }

    [Fact]
    public void CreateInstance_WithValidType_CreatesInstance()
    {
        // Arrange
        using var context = new FakeLoadContext();
        var type = typeof(TestClass);

        // Act
        var instance = context.CreateInstance(type);

        // Assert
        Assert.NotNull(instance);
        Assert.IsType<TestClass>(instance);
    }

    [Fact]
    public void CreateInstance_WithConstructorArgs_CreatesInstanceWithArgs()
    {
        // Arrange
        using var context = new FakeLoadContext();
        var type = typeof(TestClassWithConstructor);
        const string expectedValue = "test-value";

        // Act
        var instance = context.CreateInstance(type, expectedValue);

        // Assert
        Assert.NotNull(instance);
        var testInstance = Assert.IsType<TestClassWithConstructor>(instance);
        Assert.Equal(expectedValue, testInstance.Value);
    }

    [Fact]
    public void CreateInstance_Generic_CreatesInstance()
    {
        // Arrange
        using var context = new FakeLoadContext();

        // Act
        var instance = context.CreateInstance<TestClass>();

        // Assert
        Assert.NotNull(instance);
        Assert.IsType<TestClass>(instance);
    }

    [Fact]
    public void CreateInstance_Generic_WithConstructorArgs_CreatesInstanceWithArgs()
    {
        // Arrange
        using var context = new FakeLoadContext();
        const string expectedValue = "test-value";

        // Act
        var instance = context.CreateInstance<TestClassWithConstructor>(expectedValue);

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(expectedValue, instance.Value);
    }

    [Fact]
    public void CreateInstance_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = new FakeLoadContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.CreateInstance(null!));
    }

    [Fact]
    public void CreateInstance_WithAbstractType_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new FakeLoadContext();
        var type = typeof(AbstractTestClass);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => context.CreateInstance(type));
        Assert.Contains("Failed to create instance", exception.Message);
    }

    [Fact]
    public void Dispose_DisposesContext()
    {
        // Arrange
        var context = new FakeLoadContext();
        context.RegisterType("TestType", typeof(string));

        // Act
        context.Dispose();

        // Assert
        Assert.True(context.IsDisposed);
        Assert.Throws<InvalidOperationException>(() => context.Load("/fake/path.dll"));
        Assert.Throws<InvalidOperationException>(() => context.Load(typeof(string)));
        Assert.Throws<InvalidOperationException>(() => context.TryGetType("TestType", out _));
        Assert.Throws<InvalidOperationException>(() => context.CreateInstance(typeof(string)));
        Assert.Throws<InvalidOperationException>(() => context.CreateInstance<string>());
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var context = new FakeLoadContext();

        // Act & Assert - Should not throw
        context.Dispose();
        context.Dispose();
        context.Dispose();
    }

    // Test helper classes
    public class TestClass
    {
        public string Message => "Hello from TestClass";
    }

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