import { AppConfig } from '../app/types/app.data';

const serverUrl = 'http://localhost:3000';

export const environment: AppConfig = {
    apiUrls: {
        auth: `${serverUrl}/api/auth`,
        foods: `${serverUrl}/api/foods`,
        consumptions: `${serverUrl}/api/consumptions`,
        statistics: `${serverUrl}/api/statistics`,
        users: `${serverUrl}/api/users`,
        recipes: `${serverUrl}/api/recipes`,
        logs: `${serverUrl}/api/logs`,
    },
};
