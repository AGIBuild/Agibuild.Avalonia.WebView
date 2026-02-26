type BridgeRoot = {
  agWebView?: {
    rpc?: unknown;
  };
};

export function App() {
  const bridgeState = (window as unknown as BridgeRoot).agWebView?.rpc ? "connected" : "not-connected";

  return (
    <main style={{ fontFamily: "system-ui, sans-serif", margin: "2rem auto", maxWidth: 720 }}>
      <h1>HybridApp React Template</h1>
      <p>Framework path: react + vite</p>
      <p>Bridge state: {bridgeState}</p>
    </main>
  );
}
