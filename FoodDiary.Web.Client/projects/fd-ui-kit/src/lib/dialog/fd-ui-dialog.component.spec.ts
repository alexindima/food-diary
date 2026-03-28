import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { FdUiDialogComponent, FdUiDialogData } from './fd-ui-dialog.component';

describe('FdUiDialogComponent', () => {
    let component: FdUiDialogComponent;
    let fixture: ComponentFixture<FdUiDialogComponent>;
    let dialogRefSpy: jasmine.SpyObj<MatDialogRef<FdUiDialogComponent>>;

    function createComponent(data: FdUiDialogData): void {
        dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

        TestBed.configureTestingModule({
            imports: [FdUiDialogComponent],
            providers: [
                { provide: MAT_DIALOG_DATA, useValue: data },
                { provide: MatDialogRef, useValue: dialogRefSpy },
                provideNoopAnimations(),
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
        expect(titleEl!.textContent).toContain('My Title');
    });

    it('should display subtitle when provided', () => {
        createComponent({ title: 'Title', subtitle: 'My Subtitle' });
        const subtitleEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__subtitle');
        expect(subtitleEl).toBeTruthy();
        expect(subtitleEl!.textContent).toContain('My Subtitle');
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
        closeBtn!.click();
        expect(dialogRefSpy.close).toHaveBeenCalled();
    });

    it('should apply size class', () => {
        createComponent({ title: 'Title', size: 'lg' });
        const dialogEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog');
        expect(dialogEl!.classList).toContain('fd-ui-dialog--size-lg');
    });

    it('should apply default size class md when size is not provided', () => {
        createComponent({ title: 'Title' });
        const dialogEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog');
        expect(dialogEl!.classList).toContain('fd-ui-dialog--size-md');
    });

    it('should apply sm size class', () => {
        createComponent({ title: 'Title', size: 'sm' });
        const dialogEl = (fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog');
        expect(dialogEl!.classList).toContain('fd-ui-dialog--size-sm');
    });
});
