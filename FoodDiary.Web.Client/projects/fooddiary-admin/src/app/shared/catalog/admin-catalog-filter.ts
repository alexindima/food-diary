import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, type ParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';

import { adminQueryValue } from '../period/admin-query';

function matchesOptional(filter: string | null, value?: string): boolean {
    return filter === null || filter.length === 0 || filter === value;
}

export function matchesAdminCatalog(
    params: ParamMap,
    record: { text: string; locale?: string; category?: string; isActive?: boolean },
): boolean {
    const search = (params.get('search') ?? '').trim().toLocaleLowerCase();
    return (
        record.text.toLocaleLowerCase().includes(search) &&
        matchesOptional(params.get('locale'), record.locale) &&
        ((params.get('category') ?? '') === '' || params.get('category') === record.category) &&
        ((params.get('state') ?? '') === '' || (params.get('state') === 'active') === record.isActive)
    );
}

@Component({
    selector: 'fd-admin-catalog-filter',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-catalog-filter.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminCatalogFilterComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly categories = input<string[]>([]);
    public readonly showLocale = input(true);
    public readonly showState = input(false);
    public readonly activeLabel = input('ADMIN_PROMPTS.ACTIVE');
    public readonly inactiveLabel = input('ADMIN_PROMPTS.INACTIVE');
    protected readonly search = signal('');
    protected readonly locale = signal('');
    protected readonly category = signal('');
    protected readonly state = signal('');
    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.search.set(params.get('search') ?? '');
            this.locale.set(params.get('locale') ?? '');
            this.category.set(params.get('category') ?? '');
            this.state.set(params.get('state') ?? '');
        });
    }
    protected value(event: Event): string {
        const target = event.target;
        return target !== null && 'value' in target && typeof target.value === 'string' ? target.value : '';
    }
    protected apply(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                search: adminQueryValue(this.search()),
                locale: adminQueryValue(this.locale()),
                category: adminQueryValue(this.category()),
                state: adminQueryValue(this.state()),
                page: null,
            },
        });
    }
}
