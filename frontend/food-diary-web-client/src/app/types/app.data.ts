export interface AppConfig {
    apiUrls: AppConfigApiUrls
}

export interface AppConfigApiUrls {
    auth: string;
    food: string;
    user: User;
    g: ConsumedFood
}

// Основные типы данных

type User = {
    id: string;
    name: string;
    email: string;
    createdAt: Date;
    goal: Goal;
};

type Goal = {
    calories?: number;
    proteins?: number;
    fats?: number;
    carbs?: number;
    startDate: Date;
    endDate?: Date;
    gsdg: DailyStatistic;
    gdsggds: WeeklyStatistic;
    dfghdh: MonthlyStatistic;
    dhdfh: YearlyStatistic;
    dghdfh: AllTimeStatistic
};

type ConsumedFood = {
    food: Food;
    quantity: Quantity;
};

type Quantity = {
    amount: number;
    unit: Unit;
};

type Unit = 'g' | 'ml' | 'pcs';

type Food = {
    id: string;
    name: string;
    category: string;
    caloriesPer100g: number;
    proteinsPer100g: number;
    fatsPer100g: number;
    carbsPer100g: number;
    defaultServingSize: number;
};

type StatisticBase = {
    userId: string;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
};

type DailyStatistic = StatisticBase & {
    date: Date;
};

type WeeklyStatistic = StatisticBase & {
    weekStartDate: Date;
    weekEndDate: Date;
};

type MonthlyStatistic = StatisticBase & {
    month: number;
    year: number;
};

type YearlyStatistic = StatisticBase & {
    year: number;
};

type AllTimeStatistic = StatisticBase;
