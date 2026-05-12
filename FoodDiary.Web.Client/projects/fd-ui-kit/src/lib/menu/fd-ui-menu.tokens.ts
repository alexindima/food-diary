import { InjectionToken } from '@angular/core';

export interface FdUiMenuController {
    close(): void;
}

export interface FdUiMenuItemController {
    disabled(): boolean;
    focus(): void;
    isFocused(): boolean;
}

export const FD_UI_MENU = new InjectionToken<FdUiMenuController>('FD_UI_MENU');
export const FD_UI_MENU_ITEM = new InjectionToken<FdUiMenuItemController>('FD_UI_MENU_ITEM');
