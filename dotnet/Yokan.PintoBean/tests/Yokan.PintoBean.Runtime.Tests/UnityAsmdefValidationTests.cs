using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests to validate Unity assembly definition (asmdef) files are properly configured.
/// Ensures the Unity packaging meets the acceptance criteria.
/// </summary>
public class UnityAsmdefValidationTests
{
    private readonly string _repoRoot;
    
    public UnityAsmdefValidationTests()
    {
        // Find repository root by looking for the solution file
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "Yokan.PintoBean.sln")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        _repoRoot = currentDir ?? throw new InvalidOperationException("Could not find repository root");
    }

    [Fact]
    public void AllRequiredAsmdefFilesExist()
    {
        // Verify required asmdef files exist for Unity consumption
        var requiredAsmdefs = new[]
        {
            "src/Yokan.PintoBean.Abstractions/Yokan.PintoBean.Abstractions.asmdef",
            "src/Yokan.PintoBean.CodeGen/Yokan.PintoBean.CodeGen.asmdef", 
            "src/Yokan.PintoBean.Runtime/Yokan.PintoBean.Runtime.asmdef",
            "src/Yokan.PintoBean.Runtime.Unity/Yokan.PintoBean.Runtime.Unity.asmdef",
            "src/Yokan.PintoBean.Providers.Stub/Yokan.PintoBean.Providers.Stub.asmdef"
        };

        foreach (var asmdefPath in requiredAsmdefs)
        {
            var fullPath = Path.Combine(_repoRoot, asmdefPath);
            Assert.True(File.Exists(fullPath), $"Required asmdef file not found: {asmdefPath}");
        }
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Test code - JSON deserialization is needed for validation")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Test code - JSON deserialization is needed for validation")]
    public void CodeGenAsmdefIsEditorOnly()
    {
        // CodeGen should be editor-only since analyzers won't compile in Unity builds
        var codeGenAsmdefPath = Path.Combine(_repoRoot, "src/Yokan.PintoBean.CodeGen/Yokan.PintoBean.CodeGen.asmdef");
        Assert.True(File.Exists(codeGenAsmdefPath), "CodeGen asmdef file not found");
        
        var json = File.ReadAllText(codeGenAsmdefPath);
        var asmdef = JsonSerializer.Deserialize<JsonElement>(json);
        
        Assert.True(asmdef.TryGetProperty("includePlatforms", out var includePlatforms));
        var platforms = includePlatforms.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("Editor", platforms);
        
        // Verify autoReferenced is false for editor-only assemblies
        Assert.True(asmdef.TryGetProperty("autoReferenced", out var autoReferenced));
        Assert.False(autoReferenced.GetBoolean());
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Test code - JSON deserialization is needed for validation")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Test code - JSON deserialization is needed for validation")]
    public void AbstractionsHasNoReferences()
    {
        // Tier-1 (Abstractions) should have no assembly references to maintain purity
        var abstractionsAsmdefPath = Path.Combine(_repoRoot, "src/Yokan.PintoBean.Abstractions/Yokan.PintoBean.Abstractions.asmdef");
        Assert.True(File.Exists(abstractionsAsmdefPath), "Abstractions asmdef file not found");
        
        var json = File.ReadAllText(abstractionsAsmdefPath);
        var asmdef = JsonSerializer.Deserialize<JsonElement>(json);
        
        Assert.True(asmdef.TryGetProperty("references", out var references));
        var referencesArray = references.EnumerateArray().ToArray();
        Assert.Empty(referencesArray);
    }

    [Fact]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Test code - JSON deserialization is needed for validation")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Test code - JSON deserialization is needed for validation")]
    public void RuntimeReferencesOnlyAbstractions()
    {
        // Tier-3 (Runtime) should only reference Tier-1 (Abstractions)
        var runtimeAsmdefPath = Path.Combine(_repoRoot, "src/Yokan.PintoBean.Runtime/Yokan.PintoBean.Runtime.asmdef");
        Assert.True(File.Exists(runtimeAsmdefPath), "Runtime asmdef file not found");
        
        var json = File.ReadAllText(runtimeAsmdefPath);
        var asmdef = JsonSerializer.Deserialize<JsonElement>(json);
        
        Assert.True(asmdef.TryGetProperty("references", out var references));
        var referencesArray = references.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(referencesArray);
        Assert.Contains("Yokan.PintoBean.Abstractions", referencesArray);
    }

    [Fact]
    public void UpmPackageStructureExists()
    {
        // Verify UPM package structure exists
        var upmRoot = Path.Combine(_repoRoot, "../../Packages/com.giantcroissant.yokan");
        upmRoot = Path.GetFullPath(upmRoot); // Normalize path
        
        Assert.True(Directory.Exists(upmRoot), "UPM package directory not found");
        Assert.True(File.Exists(Path.Combine(upmRoot, "package.json")), "package.json not found");
        Assert.True(File.Exists(Path.Combine(upmRoot, "README.md")), "README.md not found");
        
        // Verify Runtime assemblies exist in UPM package
        var runtimeDir = Path.Combine(upmRoot, "Runtime");
        Assert.True(Directory.Exists(runtimeDir), "Runtime directory not found in UPM package");
        
        var expectedDirs = new[] { "Abstractions", "Runtime", "Runtime.Unity", "Providers.Stub" };
        foreach (var dir in expectedDirs)
        {
            Assert.True(Directory.Exists(Path.Combine(runtimeDir, dir)), $"{dir} directory not found in UPM Runtime");
        }
        
        // Verify Editor assembly exists
        var editorDir = Path.Combine(upmRoot, "Editor/CodeGen");
        Assert.True(Directory.Exists(editorDir), "Editor/CodeGen directory not found in UPM package");
    }
}