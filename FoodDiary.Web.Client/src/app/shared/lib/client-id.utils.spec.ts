import { describe, expect, it } from 'vitest';

import { createClientId } from './client-id.utils';

describe('createClientId', () => {
    it('prefixes generated ids', () => {
        expect(createClientId('temp')).toMatch(/^temp-/);
    });
});
