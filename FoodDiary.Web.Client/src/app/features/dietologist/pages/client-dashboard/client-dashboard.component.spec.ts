import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DietologistService } from '../../api/dietologist.service';
import { createClient } from '../clients/dietologist-clients-lib/dietologist-clients.test-data';
import { ClientDashboardComponent } from './client-dashboard.component';

let fixture: ComponentFixture<ClientDashboardComponent>;
let component: ClientDashboardComponent;
let dietologistService: { getMyClients: ReturnType<typeof vi.fn> };
let router: { navigate: ReturnType<typeof vi.fn> };

beforeEach(() => {
    dietologistService = {
        getMyClients: vi.fn(() => of([createClient({ userId: 'client-1' })])),
    };
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
});

describe('ClientDashboardComponent', () => {
    it('loads selected client by route id', () => {
        createComponent('client-1');

        expect(component.loading()).toBe(false);
        expect(component.clientTitle()).toBe('Alex Ivanov');
        expect(component.profileChips()).toContain('180 cm');
        expect(component.visibleSections()).toHaveLength(2);
    });

    it('sets empty client when route id does not match', () => {
        createComponent('missing-client');

        expect(component.loading()).toBe(false);
        expect(component.client()).toBeNull();
    });

    it('stops loading on request error', () => {
        dietologistService.getMyClients.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent('client-1');

        expect(component.loading()).toBe(false);
        expect(component.client()).toBeNull();
    });

    it('navigates back to clients list', () => {
        createComponent('client-1');

        component.goBack();

        expect(router.navigate).toHaveBeenCalledWith(['/dietologist']);
    });
});

function createComponent(clientId: string): void {
    TestBed.configureTestingModule({
        imports: [ClientDashboardComponent, TranslateModule.forRoot()],
        providers: [
            { provide: DietologistService, useValue: dietologistService },
            { provide: Router, useValue: router },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        paramMap: convertToParamMap({ clientId }),
                    },
                },
            },
        ],
    });

    fixture = TestBed.createComponent(ClientDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}
