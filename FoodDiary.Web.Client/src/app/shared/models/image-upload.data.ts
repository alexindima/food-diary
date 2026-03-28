export interface ImageUploadUrlResponse {
    uploadUrl: string;
    fileUrl: string;
    objectKey: string;
    expiresAtUtc: string;
    assetId: string;
}

export interface ImageSelection {
    url: string | null;
    assetId: string | null;
}
