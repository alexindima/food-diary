export type RecipeDetailAction = 'Edit' | 'Delete' | 'Duplicate' | 'FavoriteChanged';

export type MacroBlock = {
    labelKey: string;
    value: number;
    unitKey: string;
    color: string;
    percent: number;
};

export type IngredientPreviewItem = {
    name: string;
    amount: number;
    unitKey: string | null;
};

export class RecipeDetailActionResult {
    public constructor(
        public id: string,
        public action: RecipeDetailAction,
        public favoriteChanged = false,
    ) {}
}
