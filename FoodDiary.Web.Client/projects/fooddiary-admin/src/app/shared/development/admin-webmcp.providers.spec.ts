import { enableProdMode, isDevMode } from '@angular/core';
import { expect, it } from 'vitest';

import { provideAdminWebmcp } from './admin-webmcp.providers';
import { provideAdminWebmcp as provideProductionAdminWebmcp } from './admin-webmcp.providers.production';

it('omits development tool providers when Angular runs in production mode', () => {
    enableProdMode();
    expect(isDevMode()).toBe(false);

    expect(provideAdminWebmcp()).toEqual([]);
    expect(provideProductionAdminWebmcp()).toEqual([]);
});
