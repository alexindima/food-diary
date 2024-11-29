import { AppConfig } from '../app/types/app.data';

export const environment: AppConfig = {
    apiUrls: {
        auth: 'http://localhost:3000/api/auth',
        foods: 'http://localhost:3000/api/foods',
        consumptions: 'http://localhost:3000/api/consumptions',
        statistics: 'http://localhost:3000/api/statistics',
        users: 'http://localhost:3000/api/users',
        logs: 'http://localhost:3000/api/logs',
    },
};
