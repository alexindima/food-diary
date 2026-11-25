import { UpperCasePipe } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    inject,
    input,
    linkedSignal,
    signal,
    untracked,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import type { ClientTask, ClientTaskStatus, DietologistRecommendation } from '../../../../shared/models/dietologist.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { RecommendationThreadComponent } from '../../components/recommendation-thread/recommendation-thread';
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
        UpperCasePipe,
        LocalizedDatePipe,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiIconComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        RecommendationThreadComponent,
    ],
    templateUrl: './recommendations-page.html',
    styleUrl: './recommendations-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecommendationsPageComponent {
    public readonly recommendationId = input<string | null, string | string[] | undefined>(null, {
        transform: value => (Array.isArray(value) ? value[0] : value) ?? null,
    });

    protected readonly locale = injectCurrentLanguage();
    private readonly destroyRef = inject(DestroyRef);
    private readonly recommendationsFacade = inject(RecommendationsFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    private readonly recommendationsResource = rxResource({ stream: () => this.recommendationsFacade.getMyRecommendations() });
    private readonly tasksResource = rxResource({ stream: () => this.recommendationsFacade.getMyTasks() });
    protected readonly recommendations = linkedSignal(() =>
        this.recommendationsResource.hasValue() ? this.recommendationsResource.value() : [],
    );
    protected readonly tasks = linkedSignal(() => (this.tasksResource.hasValue() ? this.tasksResource.value() : []));
    protected readonly tasksLoading = this.tasksResource.isLoading;
    protected readonly taskErrorKey = computed(() => (this.tasksResource.error() === undefined ? null : 'CLIENT_TASKS.LOAD_ERROR'));
    protected readonly changingTaskIds = signal<ReadonlySet<string>>(new Set<string>());
    protected readonly selectedRecommendationId = linkedSignal(this.recommendationId);
    protected readonly taskActionErrorKey = signal<string | null>(null);
    protected readonly readErrorKey = signal<string | null>(null);
    protected readonly markingReadIds = signal<ReadonlySet<string>>(new Set<string>());
    protected readonly isLoading = this.recommendationsResource.isLoading;
    protected readonly errorKey = computed(() =>
        this.recommendationsResource.error() === undefined ? null : 'RECOMMENDATIONS.LOAD_ERROR',
    );
    protected readonly recommendationItems = computed<RecommendationViewModel[]>(() =>
        this.recommendations().map(recommendation => ({
            ...recommendation,
            dietologistName: this.resolveDietologistName(recommendation),
            isSelected: recommendation.id === this.selectedRecommendationId(),
            isMarkingRead: this.markingReadIds().has(recommendation.id),
        })),
    );

    public constructor() {
        effect(() => {
            this.recommendationId();
            if (this.recommendationsResource.hasValue()) {
                this.recommendationsResource.value();
                untracked(() => {
                    this.markRecommendationIdAsRead(this.selectedRecommendationId());
                });
            }
        });
    }

    protected markRecommendationAsRead(recommendation: DietologistRecommendation): void {
        if (recommendation.isRead || this.markingReadIds().has(recommendation.id)) {
            return;
        }

        this.markingReadIds.update(ids => new Set(ids).add(recommendation.id));
        this.readErrorKey.set(null);
        this.recommendationsFacade
            .markAsRead(recommendation.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.recommendations.update(items =>
                        items.map(item =>
                            item.id === recommendation.id
                                ? { ...item, isRead: true, readAtUtc: utcInstant(new Date().toISOString()) }
                                : item,
                        ),
                    );
                    this.markingReadIds.update(ids => {
                        const nextIds = new Set(ids);
                        nextIds.delete(recommendation.id);
                        return nextIds;
                    });
                },
                error: () => {
                    this.readErrorKey.set('RECOMMENDATIONS.MARK_READ_ERROR');
                    this.markingReadIds.update(ids => {
                        const nextIds = new Set(ids);
                        nextIds.delete(recommendation.id);
                        return nextIds;
                    });
                },
            });
    }

    protected openRecommendation(recommendation: DietologistRecommendation): void {
        this.selectedRecommendationId.set(recommendation.id);
        this.markRecommendationAsRead(recommendation);
    }

    protected startRecommendationsTour(force = true): void {
        this.tourService.start(this.localizedTour.build(RECOMMENDATIONS_TOUR), { force });
    }

    protected changeTaskStatus(task: ClientTask, status: Extract<ClientTaskStatus, 'Open' | 'Completed'>): void {
        if (this.changingTaskIds().has(task.id)) {
            return;
        }

        this.changingTaskIds.update(ids => new Set(ids).add(task.id));
        this.taskActionErrorKey.set(null);
        this.recommendationsFacade
            .changeTaskStatus(task.id, status)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: updated => {
                    this.tasks.update(tasks => tasks.map(item => (item.id === updated.id ? updated : item)));
                    this.removeChangingTaskId(task.id);
                },
                error: () => {
                    this.taskActionErrorKey.set('CLIENT_TASKS.CHANGE_ERROR');
                    this.removeChangingTaskId(task.id);
                },
            });
    }

    protected retryRecommendations(): void {
        this.recommendationsResource.reload();
    }

    protected retryTasks(): void {
        this.tasksResource.reload();
    }

    private removeChangingTaskId(taskId: string): void {
        this.changingTaskIds.update(ids => {
            const next = new Set(ids);
            next.delete(taskId);
            return next;
        });
    }

    private markRecommendationIdAsRead(selectedId: string | null): void {
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
