import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
    appId: 'club.fooddiary.app',
    appName: 'Food Diary',
    webDir: '../FoodDiary.Web.Client/dist/browser',
    server: {
        androidScheme: 'https',
        appStartPath: '/mobile/login',
    },
};

export default config;
