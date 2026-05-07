import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { MainComponent } from './main.component';

describe('MainComponent', () => {
    let fixture: ComponentFixture<MainComponent>;
    let dialogServiceMock: { open: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        TestBed.resetTestingModule();
        dialogServiceMock = {
            open: vi.fn(),
        };
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    async function createComponentAsync(
        path: string,
        params: Record<string, string> = {},
        queryParams: Record<string, string> = {},
    ): Promise<void> {
        TestBed.overrideComponent(MainComponent, {
            set: {
                imports: [],
                template: '',
            },
        });

        await TestBed.configureTestingModule({
            imports: [MainComponent],
            providers: [
                { provide: FdUiDialogService, useValue: dialogServiceMock },
                {
                    provide: ActivatedRoute,
                    useValue: {
                        snapshot: {
                            routeConfig: { path },
                            params,
                            queryParamMap: {
                                get: (key: string): string | null => queryParams[key] ?? null,
                            },
                        },
                    },
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(MainComponent);
    }

    it('opens auth dialog with route query params', async () => {
        await createComponentAsync('auth/:mode', { mode: 'login' }, { returnUrl: '/dashboard', adminReturnUrl: '/users' });

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(dialogServiceMock.open).toHaveBeenCalledTimes(1);
        });
        expect(dialogServiceMock.open).toHaveBeenCalledWith(
            expect.any(Function),
            expect.objectContaining({
                preset: 'form',
                data: {
                    mode: 'login',
                    returnUrl: '/dashboard',
                    adminReturnUrl: '/users',
                },
            }),
        );
    });

    it('does not open auth dialog outside auth routes', async () => {
        await createComponentAsync('');

        fixture.detectChanges();

        expect(dialogServiceMock.open).not.toHaveBeenCalled();
    });
});
