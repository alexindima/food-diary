import { expect, it } from 'vitest';

import { WeightHistoryPageComponent } from './pages/weight-history-page/weight-history-page';
import routes from './weight-history.routes';
it('maps the feature root to its standalone history page', () => {
    expect(routes).toEqual([{ path: '', component: WeightHistoryPageComponent }]);
});
