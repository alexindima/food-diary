import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminUsersService } from '../api/admin-users.service';
import { AdminLoginActivityPageComponent } from './admin-login-activity-page';

const FIRST_PAGE = 1;
const PAGE_SIZE = 20;

describe('AdminLoginActivityPageComponent', () => {
    let component: AdminLoginActivityPageComponent;
    let fixture: ComponentFixture<AdminLoginActivityPageComponent>;
    let usersService: {
        getLoginEvents: ReturnType<typeof vi.fn>;
    };

    beforeEach(async () => {
        usersService = {
            getLoginEvents: vi.fn(() =>
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
            imports: [AdminLoginActivityPageComponent],
            providers: [{ provide: AdminUsersService, useValue: usersService }],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminLoginActivityPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create and load login activity', () => {
        expect(component).toBeTruthy();
        expect(usersService.getLoginEvents).toHaveBeenCalledWith(FIRST_PAGE, PAGE_SIZE, null);
    });
});
