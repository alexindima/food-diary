export type AppConfig = {
    apiUrls: AppConfigApiUrls;
    paddleClientToken?: string;
    googleClientId?: string;
    telegramBotUsername?: string;
    buildVersion?: string;
    adminAppUrl?: string;
    supportEmail?: string;
    siteUrls?: AppConfigSiteUrls;
    russianDefaultHosts?: readonly string[];
    enableGlobalErrorHandler?: boolean;
    enableClientObservability?: boolean;
};

export type AppConfigSiteUrls = {
    en: string;
    ru: string;
};

export type AppConfigApiUrls = {
    auth: string;
    billing: string;
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
};
