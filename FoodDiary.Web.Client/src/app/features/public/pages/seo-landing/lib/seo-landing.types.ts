import type { SeoPageSlug } from '../../../../../config/public-seo-landing-routes.config';

export type { SeoPageSlug };

export type SeoLandingPageData = {
    baseKey: string;
    featureKeys: readonly string[];
    stepKeys: readonly string[];
    faqKeys: readonly string[];
    relatedPaths: readonly SeoPageSlug[];
};

export type SeoLandingTextKeys = {
    eyebrowKey: string;
    titleKey: string;
    subtitleKey: string;
};

export type SeoLandingHeroKeys = {
    primaryActionKey: string;
    secondaryActionKey: string;
    chipKeys: readonly string[];
    panel: {
        eyebrowKey: string;
        titleKey: string;
        textKey: string;
        itemKeys: readonly string[];
    };
} & SeoLandingTextKeys;

export type SeoLandingContentItemKeys = {
    key: string;
    titleKey: string;
    textKey: string;
};

export type SeoLandingFaqItemKeys = {
    key: string;
    questionKey: string;
    answerKey: string;
};

export type SeoLandingCtaKeys = {
    titleKey: string;
    subtitleKey: string;
    primaryKey: string;
    secondaryKey: string;
};

export type SeoLandingSectionKeys = {
    items: readonly SeoLandingContentItemKeys[];
} & SeoLandingTextKeys;

export type SeoLandingFaqSectionKeys = {
    items: readonly SeoLandingFaqItemKeys[];
} & SeoLandingTextKeys;

export type SeoLandingRelatedPage = {
    path: string;
    labelKey: string;
};

export type SeoLandingViewModel = {
    baseKey: string;
    hero: SeoLandingHeroKeys;
    audience: SeoLandingSectionKeys;
    features: SeoLandingSectionKeys;
    steps: SeoLandingSectionKeys;
    faq: SeoLandingFaqSectionKeys;
    cta: SeoLandingCtaKeys;
    relatedPages: readonly SeoLandingRelatedPage[];
};
