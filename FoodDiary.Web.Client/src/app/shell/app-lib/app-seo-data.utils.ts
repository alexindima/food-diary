import type { SeoData } from '../../services/seo.service';
import { isRecord } from '../../shared/lib/unknown-value.utils';

export function parseRouteSeoData(value: unknown): SeoData | null {
    if (!isRecord(value)) {
        return null;
    }

    const seoData: SeoData = {};
    readContentSeo(value, seoData);

    if (typeof value['titleKey'] === 'string') {
        seoData.titleKey = value['titleKey'];
    }

    if (typeof value['descriptionKey'] === 'string') {
        seoData.descriptionKey = value['descriptionKey'];
    }

    if (typeof value['path'] === 'string') {
        seoData.path = value['path'];
    }

    if (typeof value['noIndex'] === 'boolean') {
        seoData.noIndex = value['noIndex'];
    }

    if (typeof value['structuredDataBaseKey'] === 'string') {
        seoData.structuredDataBaseKey = value['structuredDataBaseKey'];
    }

    if (Array.isArray(value['structuredDataFeatureKeys'])) {
        seoData.structuredDataFeatureKeys = value['structuredDataFeatureKeys'].filter(item => typeof item === 'string');
    }

    if (Array.isArray(value['structuredDataFaqKeys'])) {
        seoData.structuredDataFaqKeys = value['structuredDataFaqKeys'].filter(item => typeof item === 'string');
    }

    return seoData;
}

function readContentSeo(value: Record<string, unknown>, seoData: SeoData): void {
    for (const key of ['title', 'description', 'imageUrl'] as const) {
        if (typeof value[key] === 'string') {
            seoData[key] = value[key];
        }
    }
    if (isRecord(value['recipeStructuredData'])) {
        seoData.recipeStructuredData = value['recipeStructuredData'];
    }
}
