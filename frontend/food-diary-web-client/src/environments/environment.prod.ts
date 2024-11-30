import { AppConfig } from '../app/types/app.data';

const serverUrl = 'https://fooddiary.club';

export const environment: AppConfig = {
    apiUrls: {
        auth: `${serverUrl}/api/auth`,
        foods: `${serverUrl}/api/foods`,
        consumptions: `${serverUrl}/api/consumptions`,
        statistics: `${serverUrl}/api/statistics`,
        users: `${serverUrl}/api/users`,
        logs: `${serverUrl}/api/logs`,
    },
};
