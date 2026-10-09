import type { ProductId } from '../../../shared/models/semantics/entity-id';
export type PublicProduct = {
    id: ProductId;
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
