## 1. Core Fix — WebViewRpcService CamelCase Options

- [x] 1.1 Add `private static readonly JsonSerializerOptions` field to `WebViewRpcService` with `PropertyNamingPolicy = CamelCase` and `PropertyNameCaseInsensitive = true`
- [x] 1.2 Update `InvokeAsync(string, object?)` to use `JsonSerializer.SerializeToElement(args, _jsonOptions)` (C#→JS params serialization)
- [x] 1.3 Update `SendSuccessResponseAsync` to use `JsonSerializer.SerializeToElement(result, _jsonOptions)` (C#→JS result serialization)
- [x] 1.4 Update `InvokeAsync<T>` to use `result.Deserialize<T>(_jsonOptions)` (JS→C# typed result deserialization)

## 2. Sample Model Cleanup

- [x] 2.1 Remove redundant `[JsonPropertyName]` attributes from `PageDefinition.cs` (all properties are standard camelCase)
- [x] 2.2 Remove redundant `[JsonPropertyName]` attributes from `AppInfo.cs`
- [x] 2.3 Remove redundant `[JsonPropertyName]` attributes from `AppSettings.cs`
- [x] 2.4 Remove redundant `[JsonPropertyName]` attributes from `ChatMessage.cs`, `ChatRequest.cs`, `ChatResponse.cs`
- [x] 2.5 Remove redundant `[JsonPropertyName]` attributes from `FileEntry.cs`
- [x] 2.6 Remove redundant `[JsonPropertyName]` attributes from `RuntimeMetrics.cs`
- [x] 2.7 Remove redundant `[JsonPropertyName]` attributes from `SystemInfo.cs`
- [x] 2.8 Remove unused `using System.Text.Json.Serialization;` from cleaned model files

## 3. Tests

- [x] 3.1 Add CT: verify a plain C# record (no `[JsonPropertyName]`) serializes to camelCase when passed through `WebViewRpcService.InvokeAsync` params path
- [x] 3.2 Add CT: verify a plain C# record (no `[JsonPropertyName]`) serializes to camelCase when returned from an RPC handler (result path)
- [x] 3.3 Add CT: verify `InvokeAsync<T>` deserializes camelCase JSON from JS into a C# record without `[JsonPropertyName]`
- [x] 3.4 Add CT: verify that `[JsonPropertyName]` still takes priority over naming policy
- [x] 3.5 Run full test suite (`dotnet test`) to verify no regressions
