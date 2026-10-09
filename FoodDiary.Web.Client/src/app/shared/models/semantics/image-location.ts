/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- URL meanings preserve provider encoding and legacy locations */
import type { SemanticString, UnbrandedString } from './string-meaning';

export type PublicImageUrl = SemanticString<'public-image-url'>;
export type SignedImageUploadUrl = SemanticString<'signed-image-upload-url'>;

export function publicImageUrl(value: UnbrandedString | PublicImageUrl): PublicImageUrl {
    return value as PublicImageUrl;
}

export function signedImageUploadUrl(value: UnbrandedString | SignedImageUploadUrl): SignedImageUploadUrl {
    return value as SignedImageUploadUrl;
}
