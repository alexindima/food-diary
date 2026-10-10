import type { AppConfig } from '../app/types/app.data';
import { environment as development } from './environment.development';

// The task-owned Angular proxy selects the backend without editing source files.
const apiUrls = { ...development.apiUrls };
const isApiUrlKey = (key: string): key is keyof AppConfig['apiUrls'] => Object.hasOwn(apiUrls, key);
for (const key of Object.keys(apiUrls)) {
    if (isApiUrlKey(key)) {
        apiUrls[key] = new URL(apiUrls[key]).pathname;
    }
}

export const environment: AppConfig = {
    ...development,
    apiUrls,
    adminAppUrl: '/admin',
};
