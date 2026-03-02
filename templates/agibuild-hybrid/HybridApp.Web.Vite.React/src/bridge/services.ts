import { bridge } from "./client";

export interface GreeterService {
  greet(params: { name: string }): Promise<string>;
}

export const greeterService = bridge.getService<GreeterService>("GreeterService");
