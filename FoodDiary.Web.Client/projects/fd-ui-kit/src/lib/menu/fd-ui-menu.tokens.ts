import { InjectionToken } from '@angular/core';

export type FdUiMenuController = {
    close: () => void;
};

export type FdUiMenuItemController = {
    disabled: () => boolean;
    focus: () => void;
    isFocused: () => boolean;
};

export const FD_UI_MENU = new InjectionToken<FdUiMenuController>('FD_UI_MENU');
export const FD_UI_MENU_ITEM = new InjectionToken<FdUiMenuItemController>('FD_UI_MENU_ITEM');
