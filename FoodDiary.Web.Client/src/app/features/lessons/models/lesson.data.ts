export type LessonSummary = {
    id: string;
    title: string;
    summary?: string | null;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    isRead: boolean;
};

export type LessonPage = {
    items: LessonSummary[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    totalLessonCount: number;
    readLessonCount: number;
    availableCategories: string[];
};

export type LessonQuery = {
    locale: string;
    category?: string;
    difficulty?: string;
    search?: string;
    sort: 'recommended' | 'shortest';
    page: number;
    pageSize: number;
};

export type LessonDetail = {
    id: string;
    title: string;
    content: string;
    summary?: string | null;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    isRead: boolean;
};
