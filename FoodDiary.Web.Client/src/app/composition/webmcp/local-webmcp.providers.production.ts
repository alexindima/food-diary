import type { EnvironmentProviders, Provider } from '@angular/core';

/** Production has no development tool implementation in its dependency graph. */
export const LOCAL_WEBMCP_PROVIDERS: Array<Provider | EnvironmentProviders> = [];
