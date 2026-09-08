import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';
import { catchError, combineLatest, of, startWith, Subject, switchMap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminTemplateHistoryFacade } from '../lib/admin-template-history.facade';
import type { AdminTemplateRevision } from '../models/admin-template-revision';

@Component({
    selector: 'fd-admin-template-history',
    imports: [DatePipe, TranslatePipe, FdUiButtonComponent, AdminLoadErrorComponent],
    templateUrl: './admin-template-history.html',
    styleUrl: './admin-template-history.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminTemplateHistoryComponent {
    public readonly kind = input.required<'email-templates' | 'ai-prompts'>();
    public readonly templateKey = input('');
    public readonly locale = input('');
    public readonly refreshToken = input<string | number | null>();
    public readonly restored = output<AdminTemplateRevision>();
    private readonly api = inject(AdminTemplateHistoryFacade);
    private readonly refresh = new Subject<void>();
    protected readonly revisions = signal<AdminTemplateRevision[]>([]);
    protected readonly loading = signal(false);
    protected readonly failed = signal(false);

    public constructor() {
        const identity = toObservable(
            computed(() => ({ kind: this.kind(), key: this.templateKey(), locale: this.locale(), token: this.refreshToken() })),
        );
        combineLatest([identity, this.refresh.pipe(startWith(undefined))])
            .pipe(
                switchMap(([value]) => {
                    this.failed.set(false);
                    this.revisions.set([]);
                    this.loading.set(false);
                    if (value.key.length === 0 || value.locale.length === 0) {
                        return of([]);
                    }
                    this.loading.set(true);
                    return this.api.getRevisions(value.kind, value.key, value.locale).pipe(
                        catchError(() => {
                            this.failed.set(true);
                            return of([]);
                        }),
                    );
                }),
                takeUntilDestroyed(),
            )
            .subscribe(items => {
                this.revisions.set(items);
                this.loading.set(false);
            });
    }

    protected retry(): void {
        this.refresh.next();
    }
}
