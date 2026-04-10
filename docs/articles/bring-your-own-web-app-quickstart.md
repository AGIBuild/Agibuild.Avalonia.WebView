# Bring Your Existing Web App to Fulora — Quick Start

If you already have a React, Vue, or similar web application, you can adopt Fulora without rewriting your frontend.

The recommended brownfield path is:

```text
attach web -> dev -> package
```

Start by wiring your existing frontend into a Fulora workspace:

```bash
fulora attach web \
  --web ./app/web \
  --desktop ./app/desktop \
  --bridge ./app/bridge \
  --framework react \
  --web-command "npm run dev" \
  --dev-server-url http://localhost:5173
```

This creates Fulora-owned wiring, writes `fulora.json`, and leaves the existing web app structure intact.

1. keep your existing web project
2. run `fulora attach web` once
3. add a lightweight Avalonia + Fulora desktop host
4. connect the host to your web dev server in development
5. add native capabilities gradually through typed services

## Development model

In development, your web app continues to run through its normal dev server:

```bash
cd app/web
npm run dev
```

Your Fulora host points to it:

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173"
});

await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
```

That keeps HMR and the existing frontend workflow intact.

## Production model

In production, switch to embedded or packaged static assets:

```csharp
WebView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly
});

await WebView.NavigateAsync(new Uri("app://localhost/index.html"));
```

The same `app://localhost/...` surface works in both development and production.

## Add native capabilities only where needed

Expose host capabilities through typed bridge services:

```csharp
[JsExport]
public interface IUserProfileService
{
    Task<UserProfileDto> Get();
}
```

Use them from the frontend through the generated app-service surface:

```ts
import { services } from "./bridge/client";

const profile = await services.userProfile.get();
```

## Best practices

- keep your existing frontend framework and structure
- use the dev server in development
- use packaged assets in production
- keep generated bridge files under `src/bridge/generated`
- let app code use `services.*`, not raw RPC calls
- use preflight checks before development and packaging

## Useful commands

```bash
fulora attach web --web ./app/web --framework react
fulora dev --preflight-only
fulora package --profile desktop-public --preflight-only
fulora generate types --project ./app/bridge/MyProduct.Bridge.csproj
```

If bridge artifacts are missing or stale, run `fulora generate types` explicitly. Fulora preflight checks report drift instead of silently rewriting files.

## Next step

For the full recommended structure, integration steps, and troubleshooting guidance, see the full guide:

- [Bring Your Existing Web App to Fulora](./bring-your-own-web-app.md)
