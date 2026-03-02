/**
 * Middleware-enabled bridge client setup.
 * Configures cross-cutting concerns (logging, error normalization) before any service calls.
 */

import { createBridgeClient, withLogging, withErrorNormalization } from '@agibuild/bridge';

export const bridge = createBridgeClient();

// Development-mode middleware: log all bridge calls + normalize RPC errors
if (import.meta.env.DEV) {
  bridge.use(withLogging({ maxParamLength: 100 }));
}

bridge.use(withErrorNormalization());
