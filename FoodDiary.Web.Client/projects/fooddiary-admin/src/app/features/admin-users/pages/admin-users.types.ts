import type { AdminUserLoginDeviceSummary } from '../api/admin-users.service';

export type AdminUserLoginDeviceSummaryViewModel = {
    label: string;
} & AdminUserLoginDeviceSummary;
