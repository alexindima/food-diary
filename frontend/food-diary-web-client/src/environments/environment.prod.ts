import { AppConfig } from '../app/types/app.data';

export const environment: AppConfig = {
    apiUrls: {
        auth: 'https://fintech-food-diary-e92c7a7c2d41.herokuapp.com/api/auth',
        foods: 'https://fintech-food-diary-e92c7a7c2d41.herokuapp.com/api/foods',
        consumptions: 'https://fintech-food-diary-e92c7a7c2d41.herokuapp.com/api/consumptions',
        statistics: 'https://fintech-food-diary-e92c7a7c2d41.herokuapp.com/api/statistics',
        users: 'https://fintech-food-diary-e92c7a7c2d41.herokuapp.com/api/users',
        logs: 'https://fintech-food-diary-e92c7a7c2d41.herokuapp.com/api/logs',
    },
};
