import type { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

export const AUTH_TABS: FdUiTab[] = [
    { value: 'login', labelKey: 'AUTH.LOGIN.TITLE' },
    { value: 'register', labelKey: 'AUTH.REGISTER.TITLE' },
];

export const LOGIN_ERROR_FIELDS = ['email', 'password'] as const;
export const REGISTER_ERROR_FIELDS = ['email', 'password', 'confirmPassword'] as const;
export const PASSWORD_RESET_ERROR_FIELDS = ['email'] as const;
