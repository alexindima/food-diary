import { describe, expect, it } from 'vitest';

import { dietologistRoutes } from './dietologist.routes';

describe('dietologist routes', () => {
    it('registers clients list and client dashboard routes', () => {
        expect(dietologistRoutes.map(route => route.path)).toEqual(['', 'clients/:clientId']);
        expect(dietologistRoutes[0].data?.['seo']).toEqual({ titleKey: 'DIETOLOGIST.CLIENTS.TITLE', noIndex: true });
        expect(dietologistRoutes[1].data?.['seo']).toEqual({ titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.TITLE', noIndex: true });
    });
});
