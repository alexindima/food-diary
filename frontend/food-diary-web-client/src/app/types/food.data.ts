import { FoodFormValues } from '../components/food-container/food-manage/base-food-manage.component';

export interface Food {
    id: number;
    name: string;
    category?: string;
    caloriesPer100: number;
    proteinsPer100: number;
    fatsPer100: number;
    carbsPer100: number;
    barcode?: string | null;
    createdAt?: string;
    updatedAt?: string;
    defaultServing: number;
    defaultServingUnit: Unit;
    usageCount: number;
}

export class FoodManageDto {
    public name: string;
    public category?: string;
    public caloriesPer100: number;
    public proteinsPer100: number;
    public fatsPer100: number;
    public carbsPer100: number;
    public barcode?: string | null;
    public defaultServing: number;
    public defaultServingUnit: Unit;

    public constructor(formValue: Partial<FoodFormValues>) {
        if (!formValue.name) {
            throw new Error('Name is required');
        }

        this.name = formValue.name;
        this.category = formValue.category || undefined;
        this.caloriesPer100 = formValue.caloriesPer100 || 0;
        this.proteinsPer100 = formValue.proteinsPer100 || 0;
        this.fatsPer100 = formValue.fatsPer100 || 0;
        this.carbsPer100 = formValue.carbsPer100 || 0;
        this.barcode = formValue.barcode || null;
        this.defaultServing = formValue.defaultServing || 0;
        this.defaultServingUnit = formValue.defaultServingUnit || Unit.G;
    }
}

export class FoodFilters {
    public search?: string;

    public constructor(search: string | null) {
        if (search) {
            this.search = search;
        }
    }
}

export enum Unit {
    G = 'G',
    ML = 'ML',
    PCS = 'PCS',
}
