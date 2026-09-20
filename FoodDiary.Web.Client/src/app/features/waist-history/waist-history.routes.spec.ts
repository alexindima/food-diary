import { expect, it } from 'vitest';

import { WaistHistoryPageComponent } from './pages/waist-history-page/waist-history-page';
import routes from './waist-history.routes';
it('maps the feature root to its standalone history page', () => {
    expect(routes).toEqual([{ path: '', component: WaistHistoryPageComponent }]);
});
