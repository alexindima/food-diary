import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import { DietologistPromoComponent } from '../../components/dietologist-promo/dietologist-promo.component';
import { FeaturesComponent } from '../../components/features/features.component';
import { HeroComponent } from '../../components/hero/hero.component';
import { LandingFaqComponent } from '../../components/landing-faq/landing-faq.component';
import { LandingPreviewTourComponent } from '../../components/landing-preview-tour/landing-preview-tour.component';
import { LandingStepsComponent } from '../../components/landing-steps/landing-steps.component';

@Component({
    selector: 'fd-main',
    imports: [
        HeroComponent,
        FeaturesComponent,
        LandingPreviewTourComponent,
        LandingStepsComponent,
        DietologistPromoComponent,
        LandingFaqComponent,
    ],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.scss'],
})
export class MainComponent {
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly route = inject(ActivatedRoute);
    protected readonly faqItems = [
        {
            questionKey: 'LANDING_FAQ.ITEMS.APP_SCOPE.QUESTION',
            answerKey: 'LANDING_FAQ.ITEMS.APP_SCOPE.ANSWER',
        },
        {
            questionKey: 'LANDING_FAQ.ITEMS.PLANNING.QUESTION',
            answerKey: 'LANDING_FAQ.ITEMS.PLANNING.ANSWER',
        },
        {
            questionKey: 'LANDING_FAQ.ITEMS.PROGRESS.QUESTION',
            answerKey: 'LANDING_FAQ.ITEMS.PROGRESS.ANSWER',
        },
        {
            questionKey: 'LANDING_FAQ.ITEMS.DIETOLOGIST.QUESTION',
            answerKey: 'LANDING_FAQ.ITEMS.DIETOLOGIST.ANSWER',
        },
        {
            questionKey: 'LANDING_FAQ.ITEMS.TRACKING.QUESTION',
            answerKey: 'LANDING_FAQ.ITEMS.TRACKING.ANSWER',
        },
        {
            questionKey: 'LANDING_FAQ.ITEMS.SAFETY.QUESTION',
            answerKey: 'LANDING_FAQ.ITEMS.SAFETY.ANSWER',
        },
    ] as const;
    protected readonly seoGuides = [
        { path: '/food-diary', labelKey: 'SEO.FOOD_DIARY_PAGE' },
        { path: '/calorie-counter', labelKey: 'SEO.CALORIE_COUNTER_PAGE' },
        { path: '/meal-planner', labelKey: 'SEO.MEAL_PLANNER_PAGE' },
        { path: '/macro-tracker', labelKey: 'SEO.MACRO_TRACKER_PAGE' },
        { path: '/intermittent-fasting', labelKey: 'SEO.INTERMITTENT_FASTING_PAGE' },
        { path: '/meal-tracker', labelKey: 'SEO.MEAL_TRACKER_PAGE' },
        { path: '/weight-loss-app', labelKey: 'SEO.WEIGHT_LOSS_APP_PAGE' },
        { path: '/dietologist-collaboration', labelKey: 'SEO.DIETOLOGIST_COLLABORATION_PAGE' },
        { path: '/nutrition-planner', labelKey: 'SEO.NUTRITION_PLANNER_PAGE' },
        { path: '/weight-tracker', labelKey: 'SEO.WEIGHT_TRACKER_PAGE' },
        { path: '/body-progress-tracker', labelKey: 'SEO.BODY_PROGRESS_TRACKER_PAGE' },
        { path: '/shopping-list-for-meal-planning', labelKey: 'SEO.SHOPPING_LIST_MEAL_PLANNER_PAGE' },
        { path: '/nutrition-tracker', labelKey: 'SEO.NUTRITION_TRACKER_PAGE' },
        { path: '/food-log', labelKey: 'SEO.FOOD_LOG_PAGE' },
        { path: '/protein-tracker', labelKey: 'SEO.PROTEIN_TRACKER_PAGE' },
        { path: '/meal-prep-planner', labelKey: 'SEO.MEAL_PREP_PLANNER_PAGE' },
    ] as const;

    public constructor() {
        const path = this.route.snapshot.routeConfig?.path ?? '';
        if (path.startsWith('auth')) {
            const modeParam = this.route.snapshot.params['mode'];
            const mode: 'login' | 'register' = modeParam === 'register' ? 'register' : 'login';
            void this.openAuthDialog(mode);
        }
    }

    private async openAuthDialog(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const adminReturnUrl = this.route.snapshot.queryParamMap.get('adminReturnUrl');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            data: { mode, returnUrl, adminReturnUrl },
        });
    }
}
