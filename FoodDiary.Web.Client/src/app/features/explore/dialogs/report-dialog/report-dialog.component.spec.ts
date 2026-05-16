import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ReportService } from '../../api/report.service';
import type { ContentReport, CreateReportDto } from '../../models/report.data';
import { ReportDialogComponent, type ReportDialogData } from './report-dialog.component';
import { REPORT_REASON_MAX_LENGTH } from './report-dialog.tokens';

const REPORT_REASON_TEST_MAX_LENGTH = 12;

const dialogData: ReportDialogData = {
    targetType: 'Recipe',
    targetId: 'recipe-1',
};

let fixture: ComponentFixture<ReportDialogComponent>;
let component: ReportDialogComponent;
let reportService: ReportServiceMock;
let dialogRef: { close: ReturnType<typeof vi.fn> };
let toastService: { success: ReturnType<typeof vi.fn> };

beforeEach(() => {
    reportService = {
        create: vi.fn((dto: CreateReportDto) => of(createReport(dto))),
    };
    dialogRef = { close: vi.fn() };
    toastService = { success: vi.fn() };

    TestBed.configureTestingModule({
        imports: [ReportDialogComponent, TranslateModule.forRoot()],
        providers: [
            { provide: ReportService, useValue: reportService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: dialogData },
            { provide: FdUiToastService, useValue: toastService },
            { provide: REPORT_REASON_MAX_LENGTH, useValue: REPORT_REASON_TEST_MAX_LENGTH },
        ],
    });

    fixture = TestBed.createComponent(ReportDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
});

describe('ReportDialogComponent', () => {
    it('submits trimmed report reason and closes on success', () => {
        component.reasonControl.setValue('  Spam  ');

        component.onSubmit();

        expect(reportService.create).toHaveBeenCalledWith({
            targetType: 'Recipe',
            targetId: 'recipe-1',
            reason: 'Spam',
        });
        expect(toastService.success).toHaveBeenCalledWith('REPORT.SUCCESS');
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('does not submit blank reason', () => {
        component.reasonControl.setValue('   ');

        component.onSubmit();

        expect(reportService.create).not.toHaveBeenCalled();
    });

    it('resets submitting state on failure', () => {
        reportService.create.mockReturnValueOnce(throwError(() => new Error('failed')));
        component.reasonControl.setValue('Spam');

        component.onSubmit();

        expect(component.isSubmitting()).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('closes with false on cancel', () => {
        component.onCancel();

        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('uses injected reason max length validator', () => {
        component.reasonControl.setValue('Too long reason');

        component.onSubmit();

        expect(component.reasonControl.hasError('maxlength')).toBe(true);
        expect(reportService.create).not.toHaveBeenCalled();
    });
});

type ReportServiceMock = {
    create: ReturnType<typeof vi.fn>;
};

function createReport(dto: CreateReportDto): ContentReport {
    return {
        id: 'report-1',
        reporterId: 'user-1',
        targetType: dto.targetType,
        targetId: dto.targetId,
        reason: dto.reason,
        status: 'Pending',
        adminNote: null,
        createdAtUtc: '2026-05-16T10:00:00.000Z',
        reviewedAtUtc: null,
    };
}
