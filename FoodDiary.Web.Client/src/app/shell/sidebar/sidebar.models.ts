export type SidebarRouteItem = {
    id: string;
    icon: string;
    labelKey: string;
    route: string;
    exact?: boolean;
};

export type SidebarActionId = 'openAdminPanel' | 'openNotifications' | 'logout';

export type SidebarActionItem = {
    id: string;
    icon: string;
    labelKey: string;
    action: SidebarActionId;
    variant?: 'secondary' | 'danger';
    fill?: 'outline' | 'text';
    className?: string;
    badge?: number;
};

export type SidebarNavItem = SidebarRouteItem | SidebarActionItem;

export type DesktopSectionId = 'food' | 'body' | null;
export type MobileSheetId = 'food' | 'body' | 'reports' | 'user' | null;

export type SidebarDirectRouteRequest = {
    route: string;
    exact?: boolean;
};
