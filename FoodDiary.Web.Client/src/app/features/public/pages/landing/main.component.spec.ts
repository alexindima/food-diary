import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { PublicAuthDialogService } from '../../lib/public-auth-dialog.service';
import { MainComponent } from './main.component';

describe('MainComponent', () => {
    let fixture: ComponentFixture<MainComponent>;
    let authDialogServiceMock: { openAsync: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        TestBed.resetTestingModule();
        authDialogServiceMock = {
            openAsync: vi.fn().mockResolvedValue(undefined),
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
                { provide: PublicAuthDialogService, useValue: authDialogServiceMock },
                {
                    provide: ActivatedRoute,
                    useValue: {
                        snapshot: {
                            routeConfig: { path },
                            paramMap: convertToParamMap(params),
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
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
        });
        expect(authDialogServiceMock.openAsync).toHaveBeenCalledWith({
            mode: 'login',
            returnUrl: '/dashboard',
            adminReturnUrl: '/users',
        });
    });

    it('does not open auth dialog outside auth routes', async () => {
        await createComponentAsync('');

        fixture.detectChanges();

        expect(authDialogServiceMock.openAsync).not.toHaveBeenCalled();
    });
});
