import type { EnvironmentProviders, Provider } from '@angular/core';

/** Production has no development tool implementation in its dependency graph. */
export function provideAdminWebmcp(): Array<Provider | EnvironmentProviders> {
    return [];
}
