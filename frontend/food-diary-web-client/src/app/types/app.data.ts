export interface AppConfig {
    apiUrls: AppConfigApiUrls;
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
}
