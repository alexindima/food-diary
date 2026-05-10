import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { FdUiDialogComponent, type FdUiDialogData } from './fd-ui-dialog.component';
import { FdUiDialogHeaderDirective } from './fd-ui-dialog-header.directive';

@Component({
    standalone: true,
    imports: [FdUiDialogComponent, FdUiDialogHeaderDirective],
    template: `
        <fd-ui-dialog title="Built In Title">
            <div fdUiDialogHeader>
                <div class="custom-header">Custom Header Content</div>
            </div>
            <p>Body</p>
        </fd-ui-dialog>
    `,
})
class DialogWithCustomHeaderHostComponent {}

describe('FdUiDialogComponent', () => {
    let component: FdUiDialogComponent;
    let fixture: ComponentFixture<FdUiDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: FdUiDialogData): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [FdUiDialogComponent],
            providers: [
                { provide: FD_UI_DIALOG_DATA, useValue: data },
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
            ],
        });

        fixture = TestBed.createComponent(FdUiDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create with provided data', () => {
        createComponent({ title: 'Test Dialog' });
        expect(component).toBeTruthy();
    });

    it('should display title', () => {
        createComponent({ title: 'My Title' });
        const titleEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__title');
        expect(titleEl).toBeTruthy();
        expect(titleEl?.textContent).toContain('My Title');
    });

    it('should display subtitle when provided', () => {
        createComponent({ title: 'Title', subtitle: 'My Subtitle' });
        const subtitleEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__subtitle');
        expect(subtitleEl).toBeTruthy();
        expect(subtitleEl?.textContent).toContain('My Subtitle');
    });

    it('should not display subtitle when not provided', () => {
        createComponent({ title: 'Title' });
        const subtitleEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__subtitle');
        expect(subtitleEl).toBeNull();
    });

    it('should show dismiss button when dismissible is true (default)', () => {
        createComponent({ title: 'Title' });
        const closeBtn = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__close-button');
        expect(closeBtn).toBeTruthy();
    });

    it('should hide dismiss button when dismissible is false', () => {
        createComponent({ title: 'Title', dismissible: false });
        const closeBtn = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__close-button');
        expect(closeBtn).toBeNull();
    });

    it('should call dialogRef.close() on dismiss', () => {
        createComponent({ title: 'Title' });
        const closeBtn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button');
        expect(closeBtn).toBeTruthy();
        closeBtn?.click();
        expect(dialogRefSpy.close).toHaveBeenCalled();
    });

    it('should apply size class', () => {
        createComponent({ title: 'Title', size: 'lg' });
        const dialogEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog');
        expect(dialogEl?.classList).toContain('fd-ui-dialog--size-lg');
    });

    it('should apply default size class md when size is not provided', () => {
        createComponent({ title: 'Title' });
        const dialogEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog');
        expect(dialogEl?.classList).toContain('fd-ui-dialog--size-md');
    });

    it('should apply sm size class', () => {
        createComponent({ title: 'Title', size: 'sm' });
        const dialogEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog');
        expect(dialogEl?.classList).toContain('fd-ui-dialog--size-sm');
    });

    it('should apply xl size class', () => {
        createComponent({ title: 'Title', size: 'xl' });
        const dialogEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog');
        expect(dialogEl?.classList).toContain('fd-ui-dialog--size-xl');
    });

    it('should render custom header instead of built-in title block', () => {
        TestBed.configureTestingModule({
            imports: [DialogWithCustomHeaderHostComponent],
            providers: [],
        });

        const hostFixture = TestBed.createComponent(DialogWithCustomHeaderHostComponent);
        hostFixture.detectChanges();

        const element = hostFixture.nativeElement as HTMLElement;
        expect(element.querySelector('.custom-header')?.textContent).toContain('Custom Header Content');
        expect(element.querySelector('.fd-ui-dialog__title')).toBeNull();
        expect(element.querySelector('.fd-ui-dialog__header--custom')).toBeTruthy();
    });
});
