export type ImageUploadUrlResponse = {
    uploadUrl: string;
    fileUrl: string;
    expiresAtUtc: string;
    assetId: string;
};

export type ConfirmImageUploadResponse = {
    assetId: string;
    fileUrl: string;
};

export type ImageSelection = {
    url: string | null;
    assetId: string | null;
};
