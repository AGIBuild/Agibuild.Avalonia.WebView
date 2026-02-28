using Agibuild.Fulora.Bridge.Generator;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

/// <summary>
/// Tests that TypeScriptEmitter correctly handles nested generic types.
/// </summary>
public sealed class TypeScriptNestedGenericTests
{
    [Theory]
    [InlineData("System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>", "Record<string, number[]>")]
    [InlineData("System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, bool>>", "Record<string, boolean>[]")]
    [InlineData("System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>>", "Record<string, Record<number, string[]>>")]
    [InlineData("System.Collections.Generic.Dictionary<string, int>", "Record<string, number>")]
    [InlineData("System.Collections.Generic.List<string>", "string[]")]
    [InlineData("System.Collections.Generic.IReadOnlyList<int>", "number[]")]
    [InlineData("string", "string")]
    [InlineData("int", "number")]
    [InlineData("bool", "boolean")]
    public void CSharpTypeToTypeScript_maps_correctly(string csharpType, string expectedTs)
    {
        var result = TypeScriptEmitter.CSharpTypeToTypeScript(csharpType);
        Assert.Equal(expectedTs, result);
    }
}
