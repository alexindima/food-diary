export interface AppConfig {
    apiUrls: AppConfigApiUrls;
    googleClientId?: string;
    telegramBotUsername?: string;
    buildVersion?: string;
    adminAppUrl?: string;
    enableGlobalErrorHandler?: boolean;
    enableClientObservability?: boolean;
}

export interface AppConfigApiUrls {
    auth: string;
    products: string;
    consumptions: string;
    statistics: string;
    users: string;
    recipes: string;
    logs: string;
    weights: string;
    waists: string;
    cycles: string;
    dashboard: string;
    images: string;
    hydration: string;
    goals: string;
    ai: string;
    shoppingLists: string;
    dietologist: string;
    fasting: string;
    favoriteMeals: string;
    favoriteProducts: string;
    favoriteRecipes: string;
    gamification: string;
    weeklyCheckIn: string;
    tdee: string;
    mealPlans: string;
    exercises: string;
    lessons: string;
    usda: string;
    wearables: string;
    reports: string;
    openFoodFacts: string;
    export: string;
}
