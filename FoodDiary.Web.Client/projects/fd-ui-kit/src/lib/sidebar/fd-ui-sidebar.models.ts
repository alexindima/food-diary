export type FdUiSidebarRouteItem = {
    id: string;
    icon: string;
    label: string;
    route: string;
    exact?: boolean;
    badge?: number;
    tone?: FdUiSidebarItemTone;
};

export type FdUiSidebarActionItem = {
    id: string;
    icon: string;
    label: string;
    action: string;
    badge?: number;
    tone?: FdUiSidebarItemTone;
};

export type FdUiSidebarItem = FdUiSidebarRouteItem | FdUiSidebarActionItem;

export type FdUiSidebarSection = {
    id: string;
    title?: string;
    items: FdUiSidebarItem[];
    expanded?: boolean;
    collapsible?: boolean;
    secondary?: boolean;
};

export type FdUiSidebarItemTone = 'default' | 'brand' | 'danger';

export type FdUiSidebarRouteRequest = {
    item: FdUiSidebarRouteItem;
};

export type FdUiSidebarActionRequest = {
    item: FdUiSidebarActionItem;
};

export type FdUiSidebarSectionRequest = {
    section: FdUiSidebarSection;
};
