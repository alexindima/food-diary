import { Product } from './product.data';

export interface Consumption {
    id: number;
    userId: number;
    date: string;
    comment?: string;
    createdAt?: string;
    updatedAt?: string;
    items: ConsumptionItem[];
}

export interface ConsumptionItem {
    id: number;
    consumptionId: number;
    foodId: number;
    amount: number;
    food: Product;
}

export interface ConsumptionFilters {
    dateFrom?: string;
    dateTo?: string;
}

export interface ConsumptionManageDto {
    date: Date;
    comment?: string;
    items: ConsumptionItemManageDto[];
}

export interface ConsumptionItemManageDto {
    foodId: string; // Product ID (Guid)
    amount: number;
}
