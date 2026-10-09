import { describe, expect, it } from 'vitest';

import { imageSelection } from './image-upload.data';

describe('image selection meanings', () => {
    it('distinguishes empty, remote, confirmed asset and legacy asset-only states', () => {
        expect(imageSelection(null, null).kind).toBe('empty');
        expect(imageSelection('relative/legacy-photo', null).kind).toBe('remote');
        expect(imageSelection('https://cdn.example/photo', 'asset-1').kind).toBe('uploaded');
        expect(imageSelection(null, 'asset-1').kind).toBe('legacy-asset-only');
    });

    it('retains stored URL encoding and asset identity without trimming', () => {
        const selection = imageSelection(' relative/a%2Fb?key=X%2B%2F ', 'legacy-placeholder');
        expect(selection.url).toBe(' relative/a%2Fb?key=X%2B%2F ');
        expect(selection.assetId).toBe('legacy-placeholder');
    });
});
