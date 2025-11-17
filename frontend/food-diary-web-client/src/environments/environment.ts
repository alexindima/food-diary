import { AppConfig } from '../app/types/app.data';

const serverUrl = 'http://localhost:5300';

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
    },
};
