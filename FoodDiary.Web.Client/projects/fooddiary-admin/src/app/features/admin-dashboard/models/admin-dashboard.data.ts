export type AdminDashboardUser = {
    id: string;
    email: string;
    username?: string | null;
    firstName?: string | null;
    lastName?: string | null;
    isActive: boolean;
    createdOnUtc: string;
    deletedAt?: string | null;
    roles: string[];
};

export type AdminDashboardSummary = {
    totalUsers: number;
    activeUsers: number;
    premiumUsers: number;
    deletedUsers: number;
    recentUsers: AdminDashboardUser[];
};
