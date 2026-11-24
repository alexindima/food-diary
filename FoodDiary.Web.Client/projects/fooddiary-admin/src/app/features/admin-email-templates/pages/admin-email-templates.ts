import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import { AdminCatalogFilterComponent, matchesAdminCatalog } from '../../../shared/catalog/admin-catalog-filter';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPage, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminEmailTemplateEditDialogComponent } from '../dialogs/admin-email-template-edit-dialog';
import { AdminEmailTemplatesFacade } from '../lib/admin-email-templates.facade';
import type { AdminEmailTemplate } from '../models/admin-email-template.data';

@Component({
    selector: 'fd-admin-email-templates',
    imports: [
        AdminCatalogFilterComponent,
        AdminLoadErrorComponent,
        FdUiPaginationComponent,
        TranslatePipe,
        CommonModule,
        FdUiButtonComponent,
    ],
    templateUrl: './admin-email-templates.html',
    styleUrl: './admin-email-templates.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminEmailTemplatesComponent {
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly locale = input(undefined, { transform: firstAdminQueryValue });
    public readonly category = input(undefined, { transform: firstAdminQueryValue });
    public readonly state = input(undefined, { transform: firstAdminQueryValue });

    private readonly templatesFacade = inject(AdminEmailTemplatesFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly requestVersion = signal(0);
    private readonly templatesResource = rxResource({
        params: this.requestVersion,
        stream: () => this.templatesFacade.getAll(),
        defaultValue: [],
    });

    protected readonly templates = linkedSignal({
        source: this.templatesResource.snapshot,
        computation: (snapshot, previous): AdminEmailTemplate[] => {
            if (snapshot.status === 'error') {
                return [];
            }
            return snapshot.status === 'loading' ? (previous?.value ?? snapshot.value) : snapshot.value;
        },
    }).asReadonly();
    protected readonly isLoading = this.templatesResource.isLoading;
    protected readonly loadFailed = computed(() => this.templatesResource.error() !== undefined);

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly appliedFilters = computed(() =>
        convertToParamMap({
            page: this.page() ?? null,
            search: this.search() ?? null,
            locale: this.locale() ?? null,
            category: this.category() ?? null,
            state: this.state() ?? null,
        }),
    );
    protected readonly categories = computed(() => [...new Set(this.templates().map(item => item.key))].sort());
    protected readonly filteredItems = computed(() =>
        this.templates().filter(item =>
            matchesAdminCatalog(this.appliedFilters(), {
                text: `${item.key} ${item.subject}`,
                locale: item.locale,
                category: item.key,
                isActive: item.isActive,
            }),
        ),
    );
    protected readonly pageSize = 20;
    protected readonly requestedPage = computed(() => adminPage(this.page() ?? null) - 1);
    protected readonly pageIndex = computed(() =>
        Math.min(this.requestedPage(), Math.max(0, Math.ceil(this.filteredItems().length / this.pageSize) - 1)),
    );
    protected readonly pageItems = computed(() =>
        this.filteredItems().slice(this.pageIndex() * this.pageSize, (this.pageIndex() + 1) * this.pageSize),
    );
    protected readonly rangeStart = computed(() => (this.filteredItems().length === 0 ? 0 : this.pageIndex() * this.pageSize + 1));
    protected readonly rangeEnd = computed(() => Math.min((this.pageIndex() + 1) * this.pageSize, this.filteredItems().length));

    protected goToPage(pageIndex: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page: pageIndex + 1 }, queryParamsHandling: 'merge' });
    }

    public constructor() {
        effect(() => {
            if (!this.isLoading() && !this.loadFailed()) {
                restoreAdminPage(this.router, this.route, this.pageIndex() + 1, {
                    totalItems: this.filteredItems().length,
                    pageSize: this.pageSize,
                });
            }
        });
    }

    protected loadTemplates(): void {
        this.requestVersion.update(version => version + 1);
    }

    protected openEdit(template: AdminEmailTemplate): void {
        this.dialogService
            .open(AdminEmailTemplateEditDialogComponent, {
                preset: 'fullscreen',
                panelClass: 'fd-admin-email-template-dialog',
                data: template,
            })
            .afterClosed()
            .subscribe(updated => {
                if (updated === true) {
                    this.loadTemplates();
                }
            });
    }

    protected openCreate(): void {
        const dialogData: AdminEmailTemplate & { isNew: boolean } = {
            id: adminId<'email-template'>(''),
            key: '',
            locale: '',
            subject: '',
            htmlBody: '',
            textBody: '',
            isActive: true,
            createdOnUtc: adminUtcInstant(new Date().toISOString()),
            updatedOnUtc: null,
            isNew: true,
        };

        this.dialogService
            .open(AdminEmailTemplateEditDialogComponent, {
                preset: 'fullscreen',
                panelClass: 'fd-admin-email-template-dialog',
                data: dialogData,
            })
            .afterClosed()
            .subscribe(updated => {
                if (updated === true) {
                    this.loadTemplates();
                }
            });
    }
}
