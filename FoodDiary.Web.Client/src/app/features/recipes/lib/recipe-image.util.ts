export function resolveRecipeImageUrl(imageUrl: string | null | undefined): string | undefined {
    if (imageUrl && imageUrl.trim().length > 0) {
        return imageUrl;
    }
    return 'assets/images/stubs/receipt.png';
}
