import { createAngularMetadataCoverageFilter } from './scripts/angular-metadata-coverage.mjs';
import baseline from './vitest.config.mjs';

export default {
    ...baseline,
    test: {
        ...baseline.test,
        coverage: {
            ...baseline.test.coverage,
            ignoreNode: createAngularMetadataCoverageFilter(),
        },
    },
};
