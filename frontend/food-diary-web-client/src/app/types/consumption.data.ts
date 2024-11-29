import { Food } from './food.data';

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
    food: Food;
}

export interface ConsumptionFilters {
    dateFrom?: string;
    dateTo?: string;
}

export class SimpleConsumption {
    public constructor(
        public id: number,
        public name: string,
        public amount: number,
    ) {}

    public static mapFrom(consumptionItem: ConsumptionItem): SimpleConsumption {
        return new SimpleConsumption(consumptionItem.foodId, consumptionItem.food.name, consumptionItem.amount);
    }

    public static mapTo(simpleConsumption: SimpleConsumption): ConsumptionItemManageDto {
        return {
            foodId: simpleConsumption.id,
            amount: simpleConsumption.amount,
        };
    }
}

export interface ConsumptionManageDto {
    date: Date;
    comment?: string;
    items: ConsumptionItemManageDto[];
}

export interface ConsumptionItemManageDto {
    foodId: number;
    amount: number;
}
