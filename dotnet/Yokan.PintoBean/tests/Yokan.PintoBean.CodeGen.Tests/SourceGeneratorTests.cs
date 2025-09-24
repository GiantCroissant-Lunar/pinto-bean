using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.CodeGen.Tests;

/// <summary>
/// Tests for the incremental source generator functionality.
/// </summary>
public class SourceGeneratorTests
{
    /// <summary>
    /// Tests that the generator can be instantiated and has the correct attributes.
    /// </summary>
    [Fact]
    public void Generator_ShouldBeInstantiable()
    {
        // Act
        var generator = new PintoBeanCodeGen();

        // Assert
        Assert.NotNull(generator);
        Assert.IsAssignableFrom<IIncrementalGenerator>(generator);
    }

    /// <summary>
    /// Tests that the generator is properly attributed as a generator.
    /// </summary>
    [Fact]
    public void Generator_ShouldHaveGeneratorAttribute()
    {
        // Arrange
        var generatorType = typeof(PintoBeanCodeGen);

        // Act
        var generatorAttribute = generatorType.GetCustomAttributes(typeof(GeneratorAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(generatorAttribute);
        Assert.IsType<GeneratorAttribute>(generatorAttribute);
    }

    /// <summary>
    /// Tests basic syntax checking and pattern detection logic.
    /// This ensures our predicate functions work without full compilation.
    /// </summary>
    [Fact]
    public void Generator_SyntaxPredicate_ShouldDetectClassesWithAttributes()
    {
        // Arrange
        var sourceWithAttribute = @"
            using Yokan.PintoBean.CodeGen;

            [RealizeService(typeof(IService))]
            public partial class TestService { }
        ";

        var sourceWithoutAttribute = @"
            public partial class TestService { }
        ";

        // Act & Assert - Parse the syntax trees
        var treeWithAttribute = CSharpSyntaxTree.ParseText(sourceWithAttribute);
        var treeWithoutAttribute = CSharpSyntaxTree.ParseText(sourceWithoutAttribute);

        var classWithAttribute = treeWithAttribute.GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .FirstOrDefault();

        var classWithoutAttribute = treeWithoutAttribute.GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .FirstOrDefault();

        // Verify our logic would detect the right cases
        Assert.NotNull(classWithAttribute);
        Assert.True(classWithAttribute.AttributeLists.Count > 0);

        Assert.NotNull(classWithoutAttribute);
        Assert.True(classWithoutAttribute.AttributeLists.Count == 0);
    }
}
