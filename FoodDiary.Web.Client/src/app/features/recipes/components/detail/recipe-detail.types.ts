export type RecipeDetailAction = 'Edit' | 'Delete' | 'Duplicate' | 'FavoriteChanged';

export interface MacroBlock {
    labelKey: string;
    value: number;
    unitKey: string;
    color: string;
    percent: number;
}

export interface IngredientPreviewItem {
    name: string;
    amount: number;
    unitKey: string | null;
}

export class RecipeDetailActionResult {
    public constructor(
        public id: string,
        public action: RecipeDetailAction,
        public favoriteChanged = false,
    ) {}
}
