export interface DailyAdvice {
    id: string;
    locale: string;
    value: string;
    tag?: string | null;
    weight: number;
}
