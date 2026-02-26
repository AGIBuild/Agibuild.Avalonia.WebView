using Agibuild.Fulora;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class ScriptResultHelperTests
{
    [Fact]
    public void Null_input_returns_null()
    {
        Assert.Null(ScriptResultHelper.NormalizeJsonResult(null));
    }

    [Fact]
    public void Json_null_literal_returns_null()
    {
        Assert.Null(ScriptResultHelper.NormalizeJsonResult("null"));
    }

    [Fact]
    public void Json_encoded_string_is_decoded()
    {
        // WebView2 returns  "\"hello\""  for the JS expression  "hello"
        Assert.Equal("hello", ScriptResultHelper.NormalizeJsonResult("\"hello\""));
    }

    [Fact]
    public void Json_encoded_string_with_spaces_is_decoded()
    {
        Assert.Equal("E2E HTML Test", ScriptResultHelper.NormalizeJsonResult("\"E2E HTML Test\""));
    }

    [Fact]
    public void Json_encoded_string_with_escape_sequences_is_decoded()
    {
        // JS string containing a newline and tab: "line1\nline2\t"
        Assert.Equal("line1\nline2\t", ScriptResultHelper.NormalizeJsonResult("\"line1\\nline2\\t\""));
    }

    [Fact]
    public void Json_encoded_string_with_unicode_escape_is_decoded()
    {
        // JS string: "\u4f60\u597d" -> 你好
        Assert.Equal("你好", ScriptResultHelper.NormalizeJsonResult("\"\\u4f60\\u597d\""));
    }

    [Fact]
    public void Json_encoded_string_with_embedded_quotes_is_decoded()
    {
        // JS string: "he said \"hi\""
        Assert.Equal("he said \"hi\"", ScriptResultHelper.NormalizeJsonResult("\"he said \\\"hi\\\"\""));
    }

    [Fact]
    public void Json_encoded_empty_string_is_decoded()
    {
        // WebView2 returns  "\"\""  for the JS expression  ""
        Assert.Equal("", ScriptResultHelper.NormalizeJsonResult("\"\""));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("3.14")]
    [InlineData("-1")]
    public void Numeric_values_are_returned_as_is(string input)
    {
        Assert.Equal(input, ScriptResultHelper.NormalizeJsonResult(input));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void Boolean_values_are_returned_as_is(string input)
    {
        Assert.Equal(input, ScriptResultHelper.NormalizeJsonResult(input));
    }

    [Fact]
    public void Json_object_is_returned_as_is()
    {
        const string json = "{\"key\":\"value\"}";
        Assert.Equal(json, ScriptResultHelper.NormalizeJsonResult(json));
    }

    [Fact]
    public void Json_array_is_returned_as_is()
    {
        const string json = "[1,2,3]";
        Assert.Equal(json, ScriptResultHelper.NormalizeJsonResult(json));
    }

    [Fact]
    public void Undefined_literal_is_returned_as_is()
    {
        // WebView2 returns "undefined" for void expressions (not JSON "null").
        Assert.Equal("undefined", ScriptResultHelper.NormalizeJsonResult("undefined"));
    }

    [Fact]
    public void Single_quote_string_is_not_treated_as_json()
    {
        // A single quote character is NOT a JSON string delimiter.
        const string input = "'hello'";
        Assert.Equal(input, ScriptResultHelper.NormalizeJsonResult(input));
    }

    [Fact]
    public void Single_char_is_returned_as_is()
    {
        Assert.Equal("x", ScriptResultHelper.NormalizeJsonResult("x"));
    }
}
