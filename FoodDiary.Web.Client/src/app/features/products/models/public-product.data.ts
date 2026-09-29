export type PublicProduct = {
    id: string;
    name: string;
    brand: string | null;
    imageUrl: string | null;
    description?: string | null;
    images?: string[];
    baseUnit: string;
    baseAmount: number;
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
};
