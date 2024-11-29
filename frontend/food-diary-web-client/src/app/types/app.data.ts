export interface AppConfig {
    apiUrls: AppConfigApiUrls;
}

export interface AppConfigApiUrls {
    auth: string;
    foods: string;
    consumptions: string;
    statistics: string;
    users: string;
    logs: string;
}
