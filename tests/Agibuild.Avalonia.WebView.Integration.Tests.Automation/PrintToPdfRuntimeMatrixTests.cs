using Avalonia.Headless.XUnit;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

/// <summary>
/// Runtime-matrix style validation for PrintToPdf failure diagnosis.
/// Ensures we can clearly distinguish threading regressions from runtime interface mismatches.
/// </summary>
public sealed class PrintToPdfRuntimeMatrixTests
{
    [AvaloniaFact]
    public void Matrix_classification_distinguishes_threading_and_runtime_interface_failures()
    {
        var rows = new[]
        {
            new MatrixRow(
                RuntimeVersion: "121.0.0",
                Exception: new InvalidOperationException("CoreWebView2 members can only be accessed from the UI thread"),
                Expected: PrintToPdfOutcome.ThreadingViolation),
            new MatrixRow(
                RuntimeVersion: "86.0.0",
                Exception: new InvalidCastException("Unable to cast to Microsoft.Web.WebView2.Core.Raw.ICoreWebView2_2"),
                Expected: PrintToPdfOutcome.RuntimeInterfaceMismatch),
            new MatrixRow(
                RuntimeVersion: "132.0.0",
                Exception: null,
                Expected: PrintToPdfOutcome.Pass)
        };

        foreach (var row in rows)
        {
            var actual = Classify(row.Exception);
            Assert.Equal(row.Expected, actual);
        }
    }

    private static PrintToPdfOutcome Classify(Exception? exception)
    {
        if (exception is null)
        {
            return PrintToPdfOutcome.Pass;
        }

        var message = exception.ToString();
        if (message.Contains("UI thread", StringComparison.OrdinalIgnoreCase))
        {
            return PrintToPdfOutcome.ThreadingViolation;
        }

        if (exception is InvalidCastException &&
            message.Contains("ICoreWebView2_2", StringComparison.OrdinalIgnoreCase))
        {
            return PrintToPdfOutcome.RuntimeInterfaceMismatch;
        }

        return PrintToPdfOutcome.OtherFailure;
    }

    private enum PrintToPdfOutcome
    {
        Pass,
        ThreadingViolation,
        RuntimeInterfaceMismatch,
        OtherFailure
    }

    private sealed record MatrixRow(
        string RuntimeVersion,
        Exception? Exception,
        PrintToPdfOutcome Expected);
}
