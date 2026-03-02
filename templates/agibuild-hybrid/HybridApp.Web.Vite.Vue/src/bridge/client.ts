import { createBridgeClient, withLogging, withErrorNormalization } from "@agibuild/bridge";

export const bridge = createBridgeClient();

if (import.meta.env.DEV) {
  bridge.use(withLogging({ maxParamLength: 200 }));
}
bridge.use(withErrorNormalization());
