export interface AppConfig {
    apiUrls: AppConfigApiUrls;
    googleClientId?: string;
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
}
