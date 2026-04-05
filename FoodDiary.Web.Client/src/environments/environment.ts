import { AppConfig } from '../app/types/app.data';

const serverUrl = 'http://localhost:5300';

export const environment: AppConfig = {
    apiUrls: {
        auth: `${serverUrl}/api/v1/auth`,
        products: `${serverUrl}/api/v1/products`,
        consumptions: `${serverUrl}/api/v1/consumptions`,
        statistics: `${serverUrl}/api/v1/statistics`,
        users: `${serverUrl}/api/v1/users`,
        recipes: `${serverUrl}/api/v1/recipes`,
        logs: `${serverUrl}/api/v1/logs`,
        weights: `${serverUrl}/api/v1/weight-entries`,
        waists: `${serverUrl}/api/v1/waist-entries`,
        cycles: `${serverUrl}/api/v1/cycles`,
        dashboard: `${serverUrl}/api/v1/dashboard`,
        images: `${serverUrl}/api/v1/images`,
        hydration: `${serverUrl}/api/v1/hydrations`,
        goals: `${serverUrl}/api/v1/goals`,
        ai: `${serverUrl}/api/v1/ai`,
        shoppingLists: `${serverUrl}/api/v1/shopping-lists`,
        dietologist: `${serverUrl}/api/v1/dietologist`,
        fasting: `${serverUrl}/api/v1/fasting`,
        favoriteMeals: `${serverUrl}/api/v1/favorite-meals`,
        gamification: `${serverUrl}/api/v1/gamification`,
        weeklyCheckIn: `${serverUrl}/api/v1/weekly-check-in`,
    },
    googleClientId: '958507321562-8btd704hjhgsl7niklereh81utg5p780.apps.googleusercontent.com',
    telegramBotUsername: 'fooddiaryclub_bot',
    buildVersion: 'dev',
    adminAppUrl: 'http://localhost:4300',
    enableGlobalErrorHandler: false,
    enableClientObservability: false,
};
