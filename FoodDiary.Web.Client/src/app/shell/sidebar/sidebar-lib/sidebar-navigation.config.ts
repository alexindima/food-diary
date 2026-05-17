import type { SidebarRouteItem } from './sidebar.models';

export const FOOD_TRACKING_ITEMS: SidebarRouteItem[] = [
    { id: 'meals', icon: 'restaurant_menu', labelKey: 'SIDEBAR.FOOD_DIARY', route: '/meals' },
    { id: 'products', icon: 'inventory_2', labelKey: 'SIDEBAR.PRODUCTS', route: '/products' },
    { id: 'recipes', icon: 'restaurant', labelKey: 'SIDEBAR.RECIPES', route: '/recipes' },
    { id: 'meal-plans', icon: 'restaurant_menu', labelKey: 'SIDEBAR.MEAL_PLANS', route: '/meal-plans' },
    { id: 'shopping-lists', icon: 'shopping_cart', labelKey: 'SIDEBAR.SHOPPING_LIST', route: '/shopping-lists' },
    { id: 'fasting', icon: 'timer', labelKey: 'SIDEBAR.FASTING', route: '/fasting' },
];

export const BODY_TRACKING_ITEMS: SidebarRouteItem[] = [
    { id: 'weight-history', icon: 'monitor_weight', labelKey: 'SIDEBAR.WEIGHT', route: '/weight-history' },
    { id: 'waist-history', icon: 'straighten', labelKey: 'SIDEBAR.WAIST', route: '/waist-history' },
    { id: 'cycle-tracking', icon: 'favorite', labelKey: 'SIDEBAR.CYCLE', route: '/cycle-tracking' },
];

export const DESKTOP_BOTTOM_ITEMS: SidebarRouteItem[] = [
    { id: 'statistics', icon: 'bar_chart', labelKey: 'SIDEBAR.REPORTS', route: '/statistics' },
    { id: 'goals', icon: 'flag', labelKey: 'SIDEBAR.GOALS', route: '/goals' },
    { id: 'gamification', icon: 'emoji_events', labelKey: 'SIDEBAR.ACHIEVEMENTS', route: '/gamification' },
    { id: 'lessons', icon: 'school', labelKey: 'SIDEBAR.LESSONS', route: '/lessons' },
    { id: 'weekly-check-in', icon: 'assessment', labelKey: 'SIDEBAR.WEEKLY_CHECK_IN', route: '/weekly-check-in' },
];

export const MOBILE_REPORT_ITEMS: SidebarRouteItem[] = [
    { id: 'statistics', icon: 'bar_chart', labelKey: 'SIDEBAR.REPORTS', route: '/statistics' },
    { id: 'goals', icon: 'flag', labelKey: 'SIDEBAR.GOALS', route: '/goals' },
    { id: 'gamification', icon: 'emoji_events', labelKey: 'SIDEBAR.ACHIEVEMENTS', route: '/gamification' },
    { id: 'weekly-check-in', icon: 'assessment', labelKey: 'SIDEBAR.WEEKLY_CHECK_IN', route: '/weekly-check-in' },
];
