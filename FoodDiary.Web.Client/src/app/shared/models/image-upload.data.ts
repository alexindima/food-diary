export type ImageUploadUrlResponse = {
    uploadUrl: string;
    fileUrl: string;
    objectKey: string;
    expiresAtUtc: string;
    assetId: string;
};

export type ImageSelection = {
    url: string | null;
    assetId: string | null;
};
