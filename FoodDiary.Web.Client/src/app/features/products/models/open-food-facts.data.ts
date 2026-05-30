export type OpenFoodFactsProduct = {
    barcode: string;
    name: string;
    brand?: string | null;
    category?: string | null;
    imageUrl?: string | null;
    caloriesPer100G?: number | null;
    proteinsPer100G?: number | null;
    fatsPer100G?: number | null;
    carbsPer100G?: number | null;
    fiberPer100G?: number | null;
};
