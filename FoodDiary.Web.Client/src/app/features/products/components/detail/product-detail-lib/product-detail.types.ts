export class ProductDetailActionResult {
    public constructor(
        public id: string,
        public action: ProductDetailAction,
        public favoriteChanged = false,
    ) {}
}

export type ProductDetailAction = 'Edit' | 'Delete' | 'Duplicate' | 'FavoriteChanged';

export type ProductDetailTab = 'summary' | 'nutrients';
