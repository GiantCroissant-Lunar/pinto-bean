using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.CodeGen.Tests;

/// <summary>
/// Tests for the PintoBeanAnalyzer diagnostic rules SG0001-SG0005.
/// </summary>
public class PintoBeanAnalyzerTests
{
    private readonly DiagnosticAnalyzer _analyzer = new PintoBeanAnalyzer();

    #region SG0001 Tests - RealizeServiceAttribute in Tier-1

    /// <summary>
    /// Tests that SG0001 reports an error when RealizeServiceAttribute is used in a Tier-1 project.
    /// </summary>
    [Fact]
    public async Task SG0001_RealizeServiceInTier1_ShouldReportError()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Contracts;

public interface ITestService 
{
    void DoSomething();
}

[RealizeService(typeof(ITestService))]
public partial class TestService 
{
    public void DoSomething() { }
}";

        var expected = DiagnosticResult.CompilerError("SG0001")
            .WithSpan(12, 1, 12, 35)
            .WithMessage("RealizeServiceAttribute is only allowed in Tier-2 (Generated Façades), Tier-3 (Adapters), and Tier-4 (Providers). Remove this attribute from Tier-1 (Contracts/Models).");

        await VerifyAnalyzerAsync(source, "TestContracts", expected);
    }

    /// <summary>
    /// Tests that SG0001 does not report an error when RealizeServiceAttribute is used in a Tier-2 project.
    /// </summary>
    [Fact]
    public async Task SG0001_RealizeServiceInTier2_ShouldNotReportError()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Facades;

[GenerateRegistry(typeof(ITestService))]
public interface ITestService 
{
    void DoSomething();
}

[RealizeService(typeof(ITestService))]
public partial class TestService 
{
    public void DoSomething() { }
}";

        await VerifyAnalyzerAsync(source, "TestFacades");
    }

    #endregion

    #region SG0002 Tests - RealizeService without contracts

    /// <summary>
    /// Tests that SG0002 reports an error when RealizeServiceAttribute is used without contracts.
    /// </summary>
    [Fact]
    public async Task SG0002_RealizeServiceWithoutContracts_ShouldReportError()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Facades;

[RealizeService()]
public partial class TestService 
{
}";

        var expected = DiagnosticResult.CompilerError("SG0002")
            .WithSpan(7, 1, 7, 18)
            .WithMessage("RealizeServiceAttribute must specify at least one contract. This is likely a misconfiguration.");

        await VerifyAnalyzerAsync(source, "TestFacades", expected);
    }

    /// <summary>
    /// Tests that SG0002 does not report an error when RealizeServiceAttribute is used with contracts.
    /// </summary>
    [Fact]
    public async Task SG0002_RealizeServiceWithContracts_ShouldNotReportError()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Facades;

[GenerateRegistry(typeof(ITestService))]
public interface ITestService 
{
    void DoSomething();
}

[RealizeService(typeof(ITestService))]
public partial class TestService 
{
    public void DoSomething() { }
}";

        await VerifyAnalyzerAsync(source, "TestFacades");
    }

    #endregion

    #region SG0003 Tests - Missing GenerateRegistry

    /// <summary>
    /// Tests that SG0003 reports an error when a realized contract is missing GenerateRegistryAttribute.
    /// </summary>
    [Fact]
    public async Task SG0003_MissingGenerateRegistry_ShouldReportError()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Facades;

public interface ITestService 
{
    void DoSomething();
}

[RealizeService(typeof(ITestService))]
public partial class TestService 
{
    public void DoSomething() { }
}";

        var expected = DiagnosticResult.CompilerError("SG0003")
            .WithSpan(12, 1, 12, 35)
            .WithMessage("Contract 'Yokan.PintoBean.Facades.ITestService' is realized but missing GenerateRegistryAttribute. Add [GenerateRegistry(typeof(Yokan.PintoBean.Facades.ITestService))] to enable typed registry generation.");

        await VerifyAnalyzerAsync(source, "TestFacades", expected);
    }

    /// <summary>
    /// Tests that SG0003 does not report an error when a realized contract has GenerateRegistryAttribute.
    /// </summary>
    [Fact]
    public async Task SG0003_WithGenerateRegistry_ShouldNotReportError()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Facades;

[GenerateRegistry(typeof(ITestService))]
public interface ITestService 
{
    void DoSomething();
}

[RealizeService(typeof(ITestService))]
public partial class TestService 
{
    public void DoSomething() { }
}";

        await VerifyAnalyzerAsync(source, "TestFacades");
    }

    #endregion

    #region SG0005 Tests - Multi-contract façade spans categories

    /// <summary>
    /// Tests that SG0005 does not report a warning when multi-contract façade uses contracts from the same category.
    /// </summary>
    [Fact]
    public async Task SG0005_MultiContractSameCategory_ShouldNotReportWarning()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Facades;

[GenerateRegistry(typeof(IUserService))]
public interface IUserService 
{
    void CreateUser();
}

[GenerateRegistry(typeof(IAccountService))]
public interface IAccountService 
{
    void CreateAccount();
}

[RealizeService(typeof(IUserService), typeof(IAccountService))]
public partial class UserManagementService 
{
    public void CreateUser() { }
    public void CreateAccount() { }
}";

        await VerifyAnalyzerAsync(source, "TestFacades");
    }

    /// <summary>
    /// Tests that SG0005 reports a warning when multi-contract façade spans different categories.
    /// </summary>
    [Fact]
    public async Task SG0005_MultiContractDifferentCategories_ShouldReportWarning()
    {
        var source = @"
using System;
using Yokan.PintoBean.CodeGen;

namespace Yokan.PintoBean.Facades;

[GenerateRegistry(typeof(IUserService))]
public interface IUserService 
{
    void CreateUser();
}

[GenerateRegistry(typeof(IOrderService))]
public interface IOrderService 
{
    void CreateOrder();
}

[RealizeService(typeof(IUserService), typeof(IOrderService))]
public partial class MixedService 
{
    public void CreateUser() { }
    public void CreateOrder() { }
}";

        var expected = DiagnosticResult.CompilerWarning("SG0005")
            .WithSpan(20, 30, 20, 42)
            .WithMessage("Façade class realizes contracts from different categories: User Management, Commerce. Consider splitting into separate façades to encourage cohesion.");

        await VerifyAnalyzerAsync(source, "TestFacades", expected);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostics for the given source code.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="assemblyName">The assembly name to use for tier detection.</param>
    /// <param name="expected">The expected diagnostic results.</param>
    private async Task VerifyAnalyzerAsync(string source, string assemblyName, params DiagnosticResult[] expected)
    {
        var test = new AnalyzerTest(source, assemblyName, expected);
        await test.RunAsync();
    }

    /// <summary>
    /// Helper class for running analyzer tests.
    /// </summary>
    private class AnalyzerTest
    {
        private readonly string _source;
        private readonly string _assemblyName;
        private readonly DiagnosticResult[] _expected;

        public AnalyzerTest(string source, string assemblyName, params DiagnosticResult[] expected)
        {
            _source = source;
            _assemblyName = assemblyName;
            _expected = expected;
        }

        public async Task RunAsync()
        {
            var compilation = CreateCompilation(_source, _assemblyName);
            var analyzer = new PintoBeanAnalyzer();
            
            var compilationWithAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty));

            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            
            VerifyDiagnostics(diagnostics, _expected);
        }

        private static Compilation CreateCompilation(string source, string assemblyName)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            
            // Get the path to the CodeGen assembly that contains RealizeServiceAttribute
#pragma warning disable IL3000 // Assembly.Location is only supported in single-file mode on .NET 6+
            var codeGenAssemblyPath = Path.Combine(
                Path.GetDirectoryName(typeof(PintoBeanAnalyzerTests).Assembly.Location)!,
                "Yokan.PintoBean.CodeGen.dll");
#pragma warning restore IL3000
            
            var references = new List<MetadataReference>
            {
#pragma warning disable IL3000 // Assembly.Location is only supported in single-file mode on .NET 6+
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Private.CoreLib
                MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location), // System.Runtime
#pragma warning restore IL3000
            };
            
            // Add CodeGen reference if the assembly exists
            if (File.Exists(codeGenAssemblyPath))
            {
                references.Add(MetadataReference.CreateFromFile(codeGenAssemblyPath));
            }
            else
            {
                // Fallback: try to get the reference from the current context
                try
                {
#pragma warning disable IL3000 // Assembly.Location is only supported in single-file mode on .NET 6+
                    references.Add(MetadataReference.CreateFromFile(typeof(RealizeServiceAttribute).Assembly.Location));
                    references.Add(MetadataReference.CreateFromFile(typeof(GenerateRegistryAttribute).Assembly.Location));
#pragma warning restore IL3000
                }
                catch
                {
                    // If we can't load the references, the tests will fail with meaningful messages
                }
            }

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }

        private static void VerifyDiagnostics(ImmutableArray<Diagnostic> actualDiagnostics, DiagnosticResult[] expectedResults)
        {
            var expectedCount = expectedResults.Length;
            var actualCount = actualDiagnostics.Length;

            // If we expect 0 diagnostics but got some, check if they are the ones we're testing for
            if (expectedCount == 0 && actualCount > 0)
            {
                // Check if any of the actual diagnostics are ones we're specifically testing (SG0001-SG0005)
                var relevantDiagnostics = actualDiagnostics.Where(d => d.Id.StartsWith("SG000")).ToArray();
                if (relevantDiagnostics.Length == 0)
                {
                    // No relevant diagnostics, so the test passes (the extra diagnostics are compilation issues)
                    return;
                }
                // If we have relevant diagnostics but expected none, that's a real failure
                actualCount = relevantDiagnostics.Length;
                actualDiagnostics = relevantDiagnostics.ToImmutableArray();
            }

            // Filter to only SG000x diagnostics for our analyzer
            var filteredDiagnostics = actualDiagnostics.Where(d => d.Id.StartsWith("SG000")).ToImmutableArray();
            
            if (expectedCount != filteredDiagnostics.Length)
            {
                var actualDiagnosticMessages = string.Join(", ", filteredDiagnostics.Select(d => d.Id + ": " + d.GetMessage()));
                var allDiagnosticMessages = string.Join(", ", actualDiagnostics.Select(d => d.Id + ": " + d.GetMessage()));
                Assert.True(false, 
                    $"Expected {expectedCount} SG000x diagnostics but got {filteredDiagnostics.Length}. " +
                    $"Actual SG000x diagnostics: {actualDiagnosticMessages}. " +
                    $"All diagnostics: {allDiagnosticMessages}");
            }

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = filteredDiagnostics[i];
                var expected = expectedResults[i];

                Assert.Equal(expected.Id, actual.Id);
                Assert.Equal(expected.Severity, actual.Severity);
                
                if (!string.IsNullOrEmpty(expected.Message))
                {
                    Assert.Equal(expected.Message, actual.GetMessage());
                }

                if (expected.Spans.Any())
                {
                    var actualSpan = actual.Location.GetLineSpan();
                    var expectedSpan = expected.Spans.First();
                    
                    Assert.Equal(expectedSpan.StartLine, actualSpan.StartLinePosition.Line + 1);
                    Assert.Equal(expectedSpan.StartColumn, actualSpan.StartLinePosition.Character + 1);
                    Assert.Equal(expectedSpan.EndLine, actualSpan.EndLinePosition.Line + 1);
                    Assert.Equal(expectedSpan.EndColumn, actualSpan.EndLinePosition.Character + 1);
                }
            }
        }
    }

    #endregion
}

/// <summary>
/// Represents an expected diagnostic result for testing.
/// </summary>
internal class DiagnosticResult
{
    /// <summary>
    /// Gets or sets the diagnostic ID.
    /// </summary>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the diagnostic severity.
    /// </summary>
    public DiagnosticSeverity Severity { get; set; }
    
    /// <summary>
    /// Gets or sets the diagnostic message.
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the spans where the diagnostic should occur.
    /// </summary>
    public DiagnosticResultLocation[] Spans { get; set; } = new DiagnosticResultLocation[0];

    /// <summary>
    /// Creates a compiler error diagnostic result.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID.</param>
    /// <returns>A new DiagnosticResult for an error.</returns>
    public static DiagnosticResult CompilerError(string diagnosticId)
    {
        return new DiagnosticResult
        {
            Id = diagnosticId,
            Severity = DiagnosticSeverity.Error
        };
    }

    /// <summary>
    /// Creates a compiler warning diagnostic result.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID.</param>
    /// <returns>A new DiagnosticResult for a warning.</returns>
    public static DiagnosticResult CompilerWarning(string diagnosticId)
    {
        return new DiagnosticResult
        {
            Id = diagnosticId,
            Severity = DiagnosticSeverity.Warning
        };
    }

    /// <summary>
    /// Sets the span where the diagnostic should occur.
    /// </summary>
    /// <param name="startLine">The start line (1-based).</param>
    /// <param name="startColumn">The start column (1-based).</param>
    /// <param name="endLine">The end line (1-based).</param>
    /// <param name="endColumn">The end column (1-based).</param>
    /// <returns>This DiagnosticResult for chaining.</returns>
    public DiagnosticResult WithSpan(int startLine, int startColumn, int endLine, int endColumn)
    {
        Spans = new[] { new DiagnosticResultLocation(startLine, startColumn, endLine, endColumn) };
        return this;
    }

    /// <summary>
    /// Sets the expected diagnostic message.
    /// </summary>
    /// <param name="message">The expected message.</param>
    /// <returns>This DiagnosticResult for chaining.</returns>
    public DiagnosticResult WithMessage(string message)
    {
        Message = message;
        return this;
    }
}

/// <summary>
/// Represents a location in source code for diagnostic testing.
/// </summary>
internal class DiagnosticResultLocation
{
    /// <summary>
    /// Gets the start line (1-based).
    /// </summary>
    public int StartLine { get; }
    
    /// <summary>
    /// Gets the start column (1-based).
    /// </summary>
    public int StartColumn { get; }
    
    /// <summary>
    /// Gets the end line (1-based).
    /// </summary>
    public int EndLine { get; }
    
    /// <summary>
    /// Gets the end column (1-based).
    /// </summary>
    public int EndColumn { get; }

    /// <summary>
    /// Initializes a new instance of the DiagnosticResultLocation class.
    /// </summary>
    /// <param name="startLine">The start line (1-based).</param>
    /// <param name="startColumn">The start column (1-based).</param>
    /// <param name="endLine">The end line (1-based).</param>
    /// <param name="endColumn">The end column (1-based).</param>
    public DiagnosticResultLocation(int startLine, int startColumn, int endLine, int endColumn)
    {
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}