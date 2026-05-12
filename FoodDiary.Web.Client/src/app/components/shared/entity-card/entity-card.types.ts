import type { QualityGrade } from '../../../shared/models/quality-grade.data';

export type EntityCardNutrition = {
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
};

export type EntityCardQuality = {
    score: number;
    grade: QualityGrade;
};

export type EntityCardCollageImage = {
    url: string;
    alt: string;
};

export type EntityCardCollageState = {
    images: EntityCardCollageImage[];
    count: number;
    hasImages: boolean;
};

export type EntityCardNormalizedQuality = {
    hintKey: string;
} & EntityCardQuality;

export type EntityCardPreviewInteractionState = {
    hint: string | null;
    role: string | null;
    tabIndex: string | null;
    ariaLabel: string | null;
};

export type EntityCardOwnershipIcon = 'person' | 'groups' | null;
