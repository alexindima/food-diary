export class MealDetailActionResult {
    public constructor(
        public id: string,
        public action: MealDetailAction,
        public favoriteChanged = false,
    ) {}
}

export type MealDetailAction = 'Edit' | 'Delete' | 'Repeat' | 'FavoriteChanged';

export type MealSatietyMeta = {
    emoji: string;
    title: string;
    description: string;
};

export type MealDetailItemPreview = {
    name: string;
    amount: number;
    unitKey: string | null;
    unitText: string | null;
};

export type MealMacroBlock = {
    labelKey: string;
    value: number;
    unitKey: string;
    color: string;
    percent: number;
};
