import { expect, it } from 'vitest';

import { recipePhotoSources } from './recipe-image.util';

it('keeps a private cover asset available only to the owner preview', () => {
    const assetId = 'fbdfba8d-be34-4f14-ba22-4270fa88a24b';
    const cover = { imageAssetId: assetId, imageUrl: null };

    expect(recipePhotoSources(cover, false)).toEqual([]);
    expect(recipePhotoSources(cover, true)).toEqual([{ assetId, url: null, requiresOwner: true }]);
});
