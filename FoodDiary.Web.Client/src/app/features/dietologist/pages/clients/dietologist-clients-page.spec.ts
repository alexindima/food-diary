import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Observable } from 'rxjs';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { DietologistFacade } from '../../lib/dietologist.facade';
import { createClient } from './dietologist-clients-lib/dietologist-clients.test-data';
import { DietologistClientsPageComponent } from './dietologist-clients-page';

let fixture: ComponentFixture<DietologistClientsPageComponent>;
let component: DietologistClientsPageComponent;
let dietologistService: {
    getMyClients: ReturnType<typeof vi.fn>;
    getAttentionSignals: ReturnType<typeof vi.fn>;
    setAttentionSignalState: ReturnType<typeof vi.fn>;
    bulkCreateRecommendations: ReturnType<typeof vi.fn>;
};
let router: { navigate: ReturnType<typeof vi.fn> };

beforeEach(() => {
    dietologistService = {
        getMyClients: vi.fn(() => of([createClient()])),
        getAttentionSignals: vi.fn(() => of([])),
        setAttentionSignalState: vi.fn(() => of(undefined)),
        bulkCreateRecommendations: vi.fn(),
    };
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
});

describe('DietologistClientsPageComponent', () => {
    it('loads client cards on creation', () => {
        createComponent();

        expect(component['loading']()).toBe(false);
        expect(component['clientItems']()[0].title).toBe('Alex Ivanov');
    });

    it('shows a distinct error state when loading clients fails', () => {
        dietologistService.getMyClients.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent();

        expect(component['loading']()).toBe(false);
        expect(component['loadError']()).toBe(true);
        expect(component['clientItems']()).toEqual([]);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
    });

    it('retries loading clients after an error', () => {
        dietologistService.getMyClients
            .mockReturnValueOnce(throwError(() => new Error('failed')))
            .mockReturnValueOnce(of([createClient({ userId: 'client-retry' })]));
        createComponent();

        component['retryLoad']();
        fixture.detectChanges();

        expect(dietologistService.getMyClients).toHaveBeenCalledTimes(2);
        expect(component['loadError']()).toBe(false);
        expect(component['clientItems']()[0].client.userId).toBe('client-retry');
    });

    it('navigates to selected client dashboard', () => {
        createComponent();

        component['openClient'](createClient({ userId: 'client-2' }));

        expect(router.navigate).toHaveBeenCalledWith(['/dietologist', 'clients', 'client-2']);
    });

    it('loads attention signals with configured thresholds', () => {
        createComponent();

        expect(dietologistService.getAttentionSignals).toHaveBeenCalledWith({
            inactivityDays: 3,
            calorieDeviationPercent: 25,
            sustainedDays: 3,
            weightChangePercent: 3,
            lookbackDays: 14,
        });
    });
});

function createComponent(): void {
    TestBed.configureTestingModule({
        imports: [DietologistClientsPageComponent],
        providers: [
            provideTranslateTesting(),
            { provide: DietologistFacade, useValue: dietologistService },
            { provide: Router, useValue: router },
            {
                provide: FdUiDialogService,
                useValue: { open: vi.fn(() => ({ afterClosed: (): Observable<boolean> => of(false) })) },
            },
            { provide: FdUiToastService, useValue: { success: vi.fn(), error: vi.fn() } },
        ],
    });

    fixture = TestBed.createComponent(DietologistClientsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}
