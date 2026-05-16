import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DietologistService } from '../../api/dietologist.service';
import { createClient } from './dietologist-clients-lib/dietologist-clients.mapper.spec';
import { DietologistClientsPageComponent } from './dietologist-clients-page.component';

let fixture: ComponentFixture<DietologistClientsPageComponent>;
let component: DietologistClientsPageComponent;
let dietologistService: { getMyClients: ReturnType<typeof vi.fn> };
let router: { navigate: ReturnType<typeof vi.fn> };

beforeEach(() => {
    dietologistService = {
        getMyClients: vi.fn(() => of([createClient()])),
    };
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
});

describe('DietologistClientsPageComponent', () => {
    it('loads client cards on creation', () => {
        createComponent();

        expect(component.loading()).toBe(false);
        expect(component.clientItems()[0].title).toBe('Alex Ivanov');
    });

    it('stops loading on request error', () => {
        dietologistService.getMyClients.mockReturnValueOnce(throwError(() => new Error('failed')));

        createComponent();

        expect(component.loading()).toBe(false);
        expect(component.clientItems()).toEqual([]);
    });

    it('navigates to selected client dashboard', () => {
        createComponent();

        component.openClient(createClient({ userId: 'client-2' }));

        expect(router.navigate).toHaveBeenCalledWith(['/dietologist', 'clients', 'client-2']);
    });
});

function createComponent(): void {
    TestBed.configureTestingModule({
        imports: [DietologistClientsPageComponent, TranslateModule.forRoot()],
        providers: [
            { provide: DietologistService, useValue: dietologistService },
            { provide: Router, useValue: router },
        ],
    });

    fixture = TestBed.createComponent(DietologistClientsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}
