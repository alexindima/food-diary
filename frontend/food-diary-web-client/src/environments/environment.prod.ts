import { AppConfig } from '../app/types/app.data';

export const environment: AppConfig = {
    apiUrls: {
        auth: 'http://fooddiary.club/api/auth',
        foods: 'http://fooddiary.club/api/foods',
        consumptions: 'http://fooddiary.club/api/consumptions',
        statistics: 'http://fooddiary.club/api/statistics',
        users: 'http://fooddiary.club/api/users',
        logs: 'http://fooddiary.club/api/logs',
    },
};
