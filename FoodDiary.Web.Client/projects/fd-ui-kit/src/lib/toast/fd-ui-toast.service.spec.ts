import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FdUiToastService } from './fd-ui-toast.service';

describe('FdUiToastService', () => {
    let service: FdUiToastService;
    let snackBarSpy: { open: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        snackBarSpy = { open: vi.fn() } as any;

        TestBed.configureTestingModule({
            providers: [
                FdUiToastService,
                { provide: MatSnackBar, useValue: snackBarSpy },
            ],
        });

        service = TestBed.inject(FdUiToastService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should open snackbar with message', () => {
        service.open('Hello');
        expect(snackBarSpy.open).toHaveBeenCalled();
        const args = snackBarSpy.open.mock.lastCall!;
        expect(args[0]).toBe('Hello');
    });

    it('should apply appearance panel class', () => {
        service.open('Error occurred', { appearance: 'negative' });
        const config = snackBarSpy.open.mock.lastCall![2];
        expect(config!.panelClass).toContain('fd-ui-toast--negative');
    });

    it('should include base panel class', () => {
        service.open('Info', { appearance: 'info' });
        const config = snackBarSpy.open.mock.lastCall![2];
        expect(config!.panelClass).toContain('fd-ui-toast');
        expect(config!.panelClass).toContain('fd-ui-toast--info');
    });

    it('should use default appearance class when no appearance specified', () => {
        service.open('Default message');
        const config = snackBarSpy.open.mock.lastCall![2];
        expect(config!.panelClass).toContain('fd-ui-toast--default');
    });

    it('should use default duration of 5000', () => {
        service.open('Message');
        const config = snackBarSpy.open.mock.lastCall![2];
        expect(config!.duration).toBe(5000);
    });

    it('should use custom duration', () => {
        service.open('Message', { duration: 8000 });
        const config = snackBarSpy.open.mock.lastCall![2];
        expect(config!.duration).toBe(8000);
    });

    it('should pass action text', () => {
        service.open('Deleted', { action: 'Undo' });
        const args = snackBarSpy.open.mock.lastCall!;
        expect(args[1]).toBe('Undo');
    });

    it('should pass undefined action when not provided', () => {
        service.open('Message');
        const args = snackBarSpy.open.mock.lastCall!;
        expect(args[1]).toBeUndefined();
    });

    it('should set horizontal position to center by default', () => {
        service.open('Message');
        const config = snackBarSpy.open.mock.lastCall![2];
        expect(config!.horizontalPosition).toBe('center');
    });

    it('should set vertical position to top by default', () => {
        service.open('Message');
        const config = snackBarSpy.open.mock.lastCall![2];
        expect(config!.verticalPosition).toBe('top');
    });
});
