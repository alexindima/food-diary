export interface AppConfig {
    apiUrls: AppConfigApiUrls
}

export interface AppConfigApiUrls {
    auth: string;
    food: string;
    user: string;
}
