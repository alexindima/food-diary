import { describe, expect, it } from 'vitest';

import routes from './explore.routes';
import { ExplorePageComponent } from './pages/explore/explore-page.component';

describe('explore routes', () => {
    it('registers explore page as the default route', () => {
        expect(routes).toEqual([
            {
                path: '',
                component: ExplorePageComponent,
            },
        ]);
    });
});
