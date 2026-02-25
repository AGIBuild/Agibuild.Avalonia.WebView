import { getAppInfo } from "./bridge/services";

async function bootstrap(): Promise<void> {
  const root = document.getElementById("app");
  if (!root) {
    return;
  }

  root.innerHTML = "<h1>Avalonia + Vue Hybrid Sample</h1><p>Loading bridge data...</p>";

  try {
    const info = await getAppInfo();
    root.innerHTML =
      `<h1>${info.name}</h1>` +
      `<p>Version: ${info.version}</p>` +
      `<p>${info.description}</p>`;
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    root.innerHTML = `<h1>Avalonia + Vue Hybrid Sample</h1><p>Bridge error: ${message}</p>`;
  }
}

void bootstrap();
