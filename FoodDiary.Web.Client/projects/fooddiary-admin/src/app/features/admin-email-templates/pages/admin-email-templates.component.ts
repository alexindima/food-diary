import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import { AdminEmailTemplatesService } from '../api/admin-email-templates.service';
import { AdminEmailTemplateEditDialogComponent } from '../dialogs/admin-email-template-edit-dialog.component';
import { AdminEmailTemplate } from '../models/admin-email-template.data';

@Component({
    selector: 'fd-admin-email-templates',
    standalone: true,
    imports: [CommonModule, FdUiButtonComponent],
    templateUrl: './admin-email-templates.component.html',
    styleUrl: './admin-email-templates.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminEmailTemplatesComponent {
    private readonly templatesService = inject(AdminEmailTemplatesService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly templates = signal<AdminEmailTemplate[]>([]);
    public readonly isLoading = signal(false);

    public constructor() {
        this.loadTemplates();
    }

    public loadTemplates(): void {
        this.isLoading.set(true);
        this.templatesService
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

    public openEdit(template: AdminEmailTemplate): void {
        this.dialogService
            .open(AdminEmailTemplateEditDialogComponent, {
                preset: 'fullscreen',
                panelClass: 'fd-admin-email-template-dialog',
                data: template,
            })
            .afterClosed()
            .subscribe(updated => {
                if (updated) {
                    this.loadTemplates();
                }
            });
    }

    public openCreate(): void {
        const dialogData: AdminEmailTemplate & { isNew: boolean } = {
            id: '',
            key: 'email_verification',
            locale: 'en',
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
                if (updated) {
                    this.loadTemplates();
                }
            });
    }
}
