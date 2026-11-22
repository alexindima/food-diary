import { type Recipe, RecipeVisibility } from '../models/recipe.data';
import type { ImagePreviewSource } from './image-owner-preview';

export function resolveRecipeImageUrl(imageUrl: string | null | undefined): string | undefined {
    if (imageUrl !== null && imageUrl !== undefined && imageUrl.trim().length > 0) {
        return imageUrl;
    }
    return undefined;
}

type RecipeImageOwner = Pick<Recipe, 'isOwnedByCurrentUser'> & Partial<Pick<Recipe, 'visibility' | 'publicationStatus'>>;

export type RecipePhotoData = {
    imageUrl?: string | null;
    imageAssetId?: string | null;
    images?: ReadonlyArray<{ imageUrl: string; imageAssetId?: string | null }>;
};

export function recipeUsesOwnerImagePreviews(recipe: RecipeImageOwner): boolean {
    const status = recipe.publicationStatus;
    return recipe.isOwnedByCurrentUser && (recipe.visibility === RecipeVisibility.Private || (status !== null && status !== undefined));
}

export function recipePhotoSources(photos: RecipePhotoData, requiresOwner: boolean): ImagePreviewSource[] {
    const primaryAssetId = photos.imageAssetId ?? photos.images?.find(image => image.imageUrl === photos.imageUrl)?.imageAssetId;
    const gallery = photos.images?.map(image => ({ url: image.imageUrl, assetId: image.imageAssetId, requiresOwner })) ?? [];
    return distinctRecipePhotoSources([{ url: photos.imageUrl, assetId: primaryAssetId, requiresOwner }, ...gallery]);
}

export function distinctRecipePhotoSources(sources: readonly ImagePreviewSource[]): ImagePreviewSource[] {
    const byAsset = new Map<string, ImagePreviewSource>();
    for (const source of sources) {
        const url = resolveRecipeImageUrl(source.url);
        const key = url ?? source.assetId ?? undefined;
        if (key !== undefined && !byAsset.has(key) && (url !== undefined || source.requiresOwner === true)) {
            byAsset.set(key, source);
        }
    }
    return [...byAsset.values()];
}
