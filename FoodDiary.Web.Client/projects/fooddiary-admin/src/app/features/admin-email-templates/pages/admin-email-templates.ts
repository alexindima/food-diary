import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import { AdminEmailTemplateEditDialogComponent } from '../dialogs/admin-email-template-edit-dialog';
import { AdminEmailTemplatesFacade } from '../lib/admin-email-templates.facade';
import type { AdminEmailTemplate } from '../models/admin-email-template.data';

@Component({
    selector: 'fd-admin-email-templates',
    imports: [FdUiPaginationComponent, TranslatePipe, CommonModule, FdUiButtonComponent],
    templateUrl: './admin-email-templates.html',
    styleUrl: './admin-email-templates.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminEmailTemplatesComponent {
    private readonly templatesFacade = inject(AdminEmailTemplatesFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly templates = signal<AdminEmailTemplate[]>([]);
    protected readonly isLoading = signal(false);

    protected readonly pageSize = 20;
    protected readonly requestedPage = signal(0);
    protected readonly pageIndex = computed(() =>
        Math.min(this.requestedPage(), Math.max(0, Math.ceil(this.templates().length / this.pageSize) - 1)),
    );
    protected readonly pageItems = computed(() =>
        this.templates().slice(this.pageIndex() * this.pageSize, (this.pageIndex() + 1) * this.pageSize),
    );
    protected readonly rangeStart = computed(() => (this.templates().length === 0 ? 0 : this.pageIndex() * this.pageSize + 1));
    protected readonly rangeEnd = computed(() => Math.min((this.pageIndex() + 1) * this.pageSize, this.templates().length));

    public constructor() {
        this.loadTemplates();
    }

    protected loadTemplates(): void {
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
