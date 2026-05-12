import type { QualityGrade } from '../../../shared/models/quality-grade.data';

export interface EntityCardNutrition {
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
}

export interface EntityCardQuality {
    score: number;
    grade: QualityGrade;
}

export interface EntityCardCollageImage {
    url: string;
    alt: string;
}

export interface EntityCardCollageState {
    images: EntityCardCollageImage[];
    count: number;
    hasImages: boolean;
}

export interface EntityCardNormalizedQuality extends EntityCardQuality {
    hintKey: string;
}

export interface EntityCardPreviewInteractionState {
    hint: string | null;
    role: string | null;
    tabIndex: string | null;
    ariaLabel: string | null;
}

export type EntityCardOwnershipIcon = 'person' | 'groups' | null;
