import type { UtcInstant } from './semantics/date-value';
import { entityId, type ImageAssetId } from './semantics/entity-id';
import { type PublicImageUrl, publicImageUrl, type SignedImageUploadUrl } from './semantics/image-location';
import type { UnbrandedString } from './semantics/string-meaning';

export type ImageUploadUrlResponse = {
    uploadUrl: SignedImageUploadUrl;
    fileUrl: PublicImageUrl;
    expiresAtUtc: UtcInstant;
    assetId: ImageAssetId;
};

export type ConfirmImageUploadResponse = {
    assetId: ImageAssetId;
    fileUrl: PublicImageUrl;
};

/** Scalar compatibility input for native forms and old stored selections. */
export type ImageSelectionFields = {
    url: string | null;
    assetId: string | null;
};

export type ImageSelection =
    | { kind: 'empty'; url: null; assetId: null }
    | { kind: 'remote'; url: PublicImageUrl; assetId: null }
    | { kind: 'uploaded'; url: PublicImageUrl; assetId: ImageAssetId }
    | { kind: 'legacy-asset-only'; url: null; assetId: ImageAssetId };

export function imageSelection(
    url: UnbrandedString | PublicImageUrl | null | undefined,
    assetId: UnbrandedString | ImageAssetId | null | undefined,
): ImageSelection {
    if (url === null || url === undefined) {
        return assetId === null || assetId === undefined
            ? { kind: 'empty', url: null, assetId: null }
            : { kind: 'legacy-asset-only', url: null, assetId: entityId<'image-asset'>(assetId) };
    }
    return assetId === null || assetId === undefined
        ? { kind: 'remote', url: publicImageUrl(url), assetId: null }
        : { kind: 'uploaded', url: publicImageUrl(url), assetId: entityId<'image-asset'>(assetId) };
}
