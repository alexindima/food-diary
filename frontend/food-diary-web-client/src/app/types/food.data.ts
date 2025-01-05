import { FoodFormValues } from '../components/food-container/food-manage/base-food-manage.component';

export interface Food {
    id: number;
    name: string;
    category?: string;
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    barcode?: string | null;
    createdAt?: string;
    updatedAt?: string;
    baseAmount: number;
    baseUnit: Unit;
    usageCount: number;
}

export class FoodManageDto {
    public name: string;
    public category?: string;
    public caloriesPerBase: number;
    public proteinsPerBase: number;
    public fatsPerBase: number;
    public carbsPerBase: number;
    public barcode?: string | null;
    public baseAmount: number;
    public baseUnit: Unit;

    public constructor(formValue: Partial<FoodFormValues>) {
        if (!formValue.name) {
            throw new Error('Name is required');
        }

        this.name = formValue.name;
        this.category = formValue.category || undefined;
        this.caloriesPerBase = formValue.caloriesPerBase || 0;
        this.proteinsPerBase = formValue.proteinsPerBase || 0;
        this.fatsPerBase = formValue.fatsPerBase || 0;
        this.carbsPerBase = formValue.carbsPerBase || 0;
        this.barcode = formValue.barcode || null;
        this.baseAmount = formValue.baseAmount || 100;
        this.baseUnit = formValue.baseUnit || Unit.G;
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
