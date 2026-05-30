import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminUsersService } from '../api/admin-users.service';
import { AdminImpersonationSessionsPageComponent } from './admin-impersonation-sessions-page';

const FIRST_PAGE = 1;
const PAGE_SIZE = 20;

describe('AdminImpersonationSessionsPageComponent', () => {
    let component: AdminImpersonationSessionsPageComponent;
    let fixture: ComponentFixture<AdminImpersonationSessionsPageComponent>;
    let usersService: {
        getImpersonationSessions: ReturnType<typeof vi.fn>;
    };

    beforeEach(async () => {
        usersService = {
            getImpersonationSessions: vi.fn(() =>
                of({
                    items: [],
                    page: FIRST_PAGE,
                    limit: PAGE_SIZE,
                    totalPages: FIRST_PAGE,
                    totalItems: 0,
                }),
            ),
        };

        await TestBed.configureTestingModule({
            imports: [AdminImpersonationSessionsPageComponent],
            providers: [{ provide: AdminUsersService, useValue: usersService }],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminImpersonationSessionsPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create and load impersonation sessions', () => {
        expect(component).toBeTruthy();
        expect(usersService.getImpersonationSessions).toHaveBeenCalledWith(FIRST_PAGE, PAGE_SIZE, null);
    });
});
