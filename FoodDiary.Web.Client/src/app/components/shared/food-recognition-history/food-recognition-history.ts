import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { catchError, of } from 'rxjs';

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
    public readonly recognitionSelected = output<FoodRecognitionJob>();

    protected load(): void {
        this.facade
            .listRecognitions()
            .pipe(
                catchError(() => of([])),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(jobs => {
                this.jobs.set(jobs);
                this.loaded.set(true);
            });
    }
}
