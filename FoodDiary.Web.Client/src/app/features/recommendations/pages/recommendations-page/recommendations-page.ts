import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import type { DietologistRecommendation } from '../../../../shared/models/dietologist.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { RecommendationsFacade } from '../../lib/recommendations.facade';
import { RECOMMENDATIONS_TOUR } from './recommendations-tour';

type RecommendationViewModel = DietologistRecommendation & {
    dietologistName: string | null;
    isSelected: boolean;
    isMarkingRead: boolean;
};

@Component({
    selector: 'fd-recommendations-page',
    imports: [
        DatePipe,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiIconComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
    ],
    templateUrl: './recommendations-page.html',
    styleUrl: './recommendations-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecommendationsPageComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private readonly recommendationsFacade = inject(RecommendationsFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    protected readonly recommendations = signal<DietologistRecommendation[]>([]);
    protected readonly selectedRecommendationId = signal<string | null>(null);
    protected readonly markingReadIds = signal<ReadonlySet<string>>(new Set<string>());
    protected readonly isLoading = signal(true);
    protected readonly errorKey = signal<string | null>(null);
    protected readonly recommendationItems = computed<RecommendationViewModel[]>(() =>
        this.recommendations().map(recommendation => ({
            ...recommendation,
            dietologistName: this.resolveDietologistName(recommendation),
            isSelected: recommendation.id === this.selectedRecommendationId(),
            isMarkingRead: this.markingReadIds().has(recommendation.id),
        })),
    );

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            this.selectedRecommendationId.set(params.get('recommendationId'));
            this.markSelectedRecommendationAsRead();
        });

        this.loadRecommendations();
    }

    protected markRecommendationAsRead(recommendation: DietologistRecommendation): void {
        if (recommendation.isRead || this.markingReadIds().has(recommendation.id)) {
            return;
        }

        this.markingReadIds.update(ids => new Set(ids).add(recommendation.id));
        this.recommendationsFacade
            .markAsRead(recommendation.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.recommendations.update(items =>
                        items.map(item =>
                            item.id === recommendation.id ? { ...item, isRead: true, readAtUtc: new Date().toISOString() } : item,
                        ),
                    );
                    this.markingReadIds.update(ids => {
                        const nextIds = new Set(ids);
                        nextIds.delete(recommendation.id);
                        return nextIds;
                    });
                },
                error: () => {
                    this.errorKey.set('RECOMMENDATIONS.MARK_READ_ERROR');
                    this.markingReadIds.update(ids => {
                        const nextIds = new Set(ids);
                        nextIds.delete(recommendation.id);
                        return nextIds;
                    });
                },
            });
    }

    protected startRecommendationsTour(force = true): void {
        this.tourService.start(this.localizedTour.build(RECOMMENDATIONS_TOUR), { force });
    }

    private loadRecommendations(): void {
        this.isLoading.set(true);
        this.errorKey.set(null);
        this.recommendationsFacade
            .getMyRecommendations()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: recommendations => {
                    this.recommendations.set(recommendations);
                    this.isLoading.set(false);
                    this.markSelectedRecommendationAsRead();
                },
                error: () => {
                    this.recommendations.set([]);
                    this.isLoading.set(false);
                    this.errorKey.set('RECOMMENDATIONS.LOAD_ERROR');
                },
            });
    }

    private markSelectedRecommendationAsRead(): void {
        const selectedId = this.selectedRecommendationId();
        if (selectedId === null) {
            return;
        }

        const recommendation = this.recommendations().find(item => item.id === selectedId);
        if (recommendation === undefined) {
            return;
        }

        this.markRecommendationAsRead(recommendation);
    }

    private resolveDietologistName(recommendation: DietologistRecommendation): string | null {
        const name = `${recommendation.dietologistFirstName ?? ''} ${recommendation.dietologistLastName ?? ''}`.trim();
        return name.length > 0 ? name : null;
    }
}
