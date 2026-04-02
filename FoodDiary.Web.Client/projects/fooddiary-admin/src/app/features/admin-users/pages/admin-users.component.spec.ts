import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { AdminUsersComponent } from './admin-users.component';
import { AdminUsersService } from '../api/admin-users.service';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

describe('AdminUsersComponent', () => {
    let component: AdminUsersComponent;
    let fixture: ComponentFixture<AdminUsersComponent>;
    let usersService: { getUsers: ReturnType<typeof vi.fn> };
    let dialogService: { open: ReturnType<typeof vi.fn> };

    const pagedUsers = {
        items: [
            {
                id: 'u1',
                email: 'user@example.com',
                username: 'alex',
                isActive: true,
                isEmailConfirmed: true,
                createdOnUtc: '2026-01-01T00:00:00Z',
                roles: ['Admin'],
            },
        ],
        page: 1,
        limit: 20,
        totalPages: 2,
        totalItems: 21,
    };

    beforeEach(async () => {
        usersService = { getUsers: vi.fn() };
        dialogService = { open: vi.fn() };

        usersService.getUsers.mockReturnValue(of(pagedUsers));
        dialogService.open.mockReturnValue({
            afterClosed: () => of(false),
        });

        await TestBed.configureTestingModule({
            imports: [AdminUsersComponent],
            providers: [
                { provide: AdminUsersService, useValue: usersService },
                { provide: FdUiDialogService, useValue: dialogService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminUsersComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load users on init', () => {
        expect(usersService.getUsers).toHaveBeenCalledWith(1, 20, null, false);
        expect(component.users()).toEqual(pagedUsers.items as any);
        expect(component.totalPages()).toBe(2);
        expect(component.totalItems()).toBe(21);
        expect(component.isLoading()).toBe(false);
    });

    it('should update search and reload from page 1', () => {
        component.onSearchChange('john');

        expect(component.search()).toBe('john');
        expect(component.page()).toBe(1);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(1, 20, 'john', false);
    });

    it('should toggle includeDeleted and reload', () => {
        component.toggleIncludeDeleted();

        expect(component.includeDeleted()).toBe(true);
        expect(component.page()).toBe(1);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(1, 20, null, true);
    });

    it('should change page only within valid bounds', () => {
        component.goToPage(2);
        expect(component.page()).toBe(2);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(2, 20, null, false);

        const callCount = usersService.getUsers.mock.calls.length;
        component.goToPage(0);
        component.goToPage(99);
        expect(usersService.getUsers.mock.calls.length).toBe(callCount);
    });

    it('should reload users after successful dialog close', () => {
        const close$ = new Subject<boolean>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component.openEdit(pagedUsers.items[0] as any);
        close$.next(true);
        close$.complete();

        expect(dialogService.open).toHaveBeenCalled();
        expect(usersService.getUsers).toHaveBeenCalledTimes(2);
    });
});
