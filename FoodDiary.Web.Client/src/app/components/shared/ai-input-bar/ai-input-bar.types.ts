export type AiRecognitionSource = 'Text' | 'Voice' | 'Photo';

export type AiInputBarResultItem = {
    nameEn: string;
    nameLocal?: string | null;
    amount: number;
    unit: string;
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
};

export type AiInputBarResult = {
    source: AiRecognitionSource;
    imageAssetId?: string | null;
    imageUrl?: string | null;
    recognizedAtUtc: string;
    notes?: string | null;
    items: AiInputBarResultItem[];
};
