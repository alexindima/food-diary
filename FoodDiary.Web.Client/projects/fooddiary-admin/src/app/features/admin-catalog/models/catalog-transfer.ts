export type CatalogProduct = {
    id: string;
    name: string;
    barcode: string | null;
    brand: string | null;
    productType: string;
    category: string | null;
    description: string | null;
    imageUrl: string | null;
    baseUnit: string;
    baseAmount: number;
    defaultPortionAmount: number;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
};

export type CatalogIngredient = {
    productId: string | null;
    nestedRecipeId: string | null;
    amount: number;
    textName?: string | null;
    amountText?: string | null;
};

export type CatalogStep = {
    order: number;
    description: string;
    title: string | null;
    imageUrl: string | null;
    ingredients: CatalogIngredient[];
};

export type CatalogRecipe = {
    id: string;
    name: string;
    description: string | null;
    category: string | null;
    imageUrl: string | null;
    prepTime: number | null;
    cookTime: number | null;
    servings: number;
    language: string;
    languageConfirmed: boolean;
    calculateNutritionAutomatically: boolean;
    manualCalories: number | null;
    manualProteins: number | null;
    manualFats: number | null;
    manualCarbs: number | null;
    manualFiber: number | null;
    manualAlcohol: number | null;
    steps: CatalogStep[];
};

export type CatalogFile = {
    format: 'fooddiary-catalog';
    version: 1;
    products: CatalogProduct[];
    recipes: CatalogRecipe[];
};

export type CatalogKind = 'products' | 'recipes';
export type CatalogStatus = 'ready' | 'skipped' | 'invalid' | 'imported' | 'failed';

export type CatalogResult = {
    id: string;
    status: CatalogStatus;
    errors: string[];
};

export type CatalogReportRow = {
    kind: CatalogKind;
    name: string;
} & CatalogResult;
