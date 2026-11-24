import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

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
    private readonly retryVersion = signal(0);
    private readonly historyResource = rxResource({
        params: () => {
            const key = this.templateKey();
            const locale = this.locale();
            if (key.length === 0 || locale.length === 0) {
                return;
            }
            return { kind: this.kind(), key, locale, token: this.refreshToken(), retryVersion: this.retryVersion() };
        },
        stream: ({ params }) => this.api.getRevisions(params.kind, params.key, params.locale),
    });

    protected readonly revisions = computed(() => (this.historyResource.hasValue() ? this.historyResource.value() : []));
    protected readonly loading = this.historyResource.isLoading;
    protected readonly failed = computed(() => this.historyResource.error() !== undefined);

    protected retry(): void {
        this.retryVersion.update(version => version + 1);
    }
}
