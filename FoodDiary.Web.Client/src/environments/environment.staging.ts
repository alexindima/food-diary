import { AppConfig } from '../app/types/app.data';

const serverUrl = 'http://localhost:3000';

export const environment: AppConfig = {
    apiUrls: {
        auth: `${serverUrl}/api/auth`,
        products: `${serverUrl}/api/products`,
        consumptions: `${serverUrl}/api/consumptions`,
        statistics: `${serverUrl}/api/statistics`,
        users: `${serverUrl}/api/users`,
        recipes: `${serverUrl}/api/recipes`,
        logs: `${serverUrl}/api/logs`,
        weights: `${serverUrl}/api/weight-entries`,
        waists: `${serverUrl}/api/waist-entries`,
        cycles: `${serverUrl}/api/cycles`,
        dashboard: `${serverUrl}/api/dashboard`,
        images: `${serverUrl}/api/images`,
        hydration: `${serverUrl}/api/hydrations`,
        goals: `${serverUrl}/api/goals`,
    },
    googleClientId: '958507321562-8btd704hjhgsl7niklereh81utg5p780.apps.googleusercontent.com',
    buildVersion: '__BUILD_VERSION__',
    adminAppUrl: 'https://admin.fooddiary.club',
};
