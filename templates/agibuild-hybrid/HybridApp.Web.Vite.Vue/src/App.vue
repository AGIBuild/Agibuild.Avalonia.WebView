<template>
  <main style="font-family: system-ui, sans-serif; margin: 2rem auto; max-width: 720px;">
    <h1>HybridApp Vue Template</h1>
    <p>Framework path: vue + vite</p>
    <p>Bridge state: {{ bridgeState }}</p>
  </main>
</template>

<script setup lang="ts">
type BridgeRoot = {
  agWebView?: {
    rpc?: unknown;
  };
};

const bridgeState = (window as unknown as BridgeRoot).agWebView?.rpc ? "connected" : "not-connected";
</script>
