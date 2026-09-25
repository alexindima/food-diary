import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { catchError, finalize, of } from 'rxjs';

import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';

@Component({
    selector: 'fd-food-recognition-history',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [DatePipe, TranslatePipe, FdUiButtonComponent],
    templateUrl: './food-recognition-history.html',
})
export class FoodRecognitionHistoryComponent {
    private readonly facade = inject(AiFoodFacade);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly jobs = signal<FoodRecognitionJob[]>([]);
    protected readonly loaded = signal(false);
    protected readonly loading = signal(false);
    protected readonly failed = signal(false);
    public readonly compact = input(false);
    public readonly productLabel = input(false);
    public readonly disabled = input(false);
    public readonly recognitionSelected = output<FoodRecognitionJob>();

    protected load(): void {
        if (this.loading() || this.disabled()) {
            return;
        }
        this.loading.set(true);
        this.failed.set(false);
        this.facade
            .listRecognitions()
            .pipe(
                catchError(() => {
                    this.failed.set(true);
                    return of([]);
                }),
                finalize(() => {
                    this.loading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(jobs => {
                this.jobs.set(jobs.filter(job => (job.isProductLabel ?? false) === this.productLabel()));
                this.loaded.set(true);
            });
    }
}
