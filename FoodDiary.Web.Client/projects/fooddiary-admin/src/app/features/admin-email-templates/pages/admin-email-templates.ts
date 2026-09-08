import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import { AdminCatalogFilterComponent, matchesAdminCatalog } from '../../../shared/catalog/admin-catalog-filter';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPage } from '../../../shared/period/admin-query';
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
    protected readonly loadFailed = signal(false);
    private readonly templatesFacade = inject(AdminEmailTemplatesFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly templates = signal<AdminEmailTemplate[]>([]);
    protected readonly isLoading = signal(false);

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly params = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
    protected readonly categories = computed(() => [...new Set(this.templates().map(item => item.key))].sort());
    protected readonly filteredItems = computed(() =>
        this.templates().filter(item =>
            matchesAdminCatalog(this.params(), {
                text: `${item.key} ${item.subject}`,
                locale: item.locale,
                category: item.key,
                isActive: item.isActive,
            }),
        ),
    );
    protected readonly pageSize = 20;
    protected readonly requestedPage = signal(0);
    protected readonly pageIndex = computed(() =>
        Math.min(this.requestedPage(), Math.max(0, Math.ceil(this.filteredItems().length / this.pageSize) - 1)),
    );
    protected readonly pageItems = computed(() =>
        this.filteredItems().slice(this.pageIndex() * this.pageSize, (this.pageIndex() + 1) * this.pageSize),
    );
    protected readonly rangeStart = computed(() => (this.filteredItems().length === 0 ? 0 : this.pageIndex() * this.pageSize + 1));
    protected readonly rangeEnd = computed(() => Math.min((this.pageIndex() + 1) * this.pageSize, this.filteredItems().length));

    protected goToPage(pageIndex: number): void {
        this.requestedPage.set(pageIndex);
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page: pageIndex + 1 }, queryParamsHandling: 'merge' });
    }

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.requestedPage.set(adminPage(params.get('page')) - 1);
        });
        this.loadTemplates();
    }

    protected loadTemplates(): void {
        this.loadFailed.set(false);
        this.isLoading.set(true);
        this.templatesFacade
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.templates.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.templates.set([]);
                    this.isLoading.set(false);
                },
            });
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
            id: '',
            key: '',
            locale: '',
            subject: '',
            htmlBody: '',
            textBody: '',
            isActive: true,
            createdOnUtc: new Date().toISOString(),
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
