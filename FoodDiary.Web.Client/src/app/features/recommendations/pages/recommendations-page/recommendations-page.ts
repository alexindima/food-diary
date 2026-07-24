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
import { LocalizedDatePipe } from '../../../../shared/i18n/localized-date.pipe';
import type { ClientTask, ClientTaskStatus, DietologistRecommendation } from '../../../../shared/models/dietologist.data';
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
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private readonly recommendationsFacade = inject(RecommendationsFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    protected readonly recommendations = signal<DietologistRecommendation[]>([]);
    protected readonly tasks = signal<ClientTask[]>([]);
    protected readonly tasksLoading = signal(true);
    protected readonly taskErrorKey = signal<string | null>(null);
    protected readonly changingTaskIds = signal<ReadonlySet<string>>(new Set<string>());
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
        this.loadTasks();
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
        this.taskErrorKey.set(null);
        this.recommendationsFacade
            .changeTaskStatus(task.id, status)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: updated => {
                    this.tasks.update(tasks => tasks.map(item => (item.id === updated.id ? updated : item)));
                    this.removeChangingTaskId(task.id);
                },
                error: () => {
                    this.taskErrorKey.set('CLIENT_TASKS.CHANGE_ERROR');
                    this.removeChangingTaskId(task.id);
                },
            });
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

    private loadTasks(): void {
        this.tasksLoading.set(true);
        this.recommendationsFacade
            .getMyTasks()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: tasks => {
                    this.tasks.set(tasks);
                    this.tasksLoading.set(false);
                },
                error: () => {
                    this.tasks.set([]);
                    this.tasksLoading.set(false);
                    this.taskErrorKey.set('CLIENT_TASKS.LOAD_ERROR');
                },
            });
    }

    private removeChangingTaskId(taskId: string): void {
        this.changingTaskIds.update(ids => {
            const next = new Set(ids);
            next.delete(taskId);
            return next;
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
