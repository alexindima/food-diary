import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminAiUsageService } from '../../admin-ai-usage/api/admin-ai-usage.service';

describe('AdminDashboardComponent', () => {
    let component: AdminDashboardComponent;
    let fixture: ComponentFixture<AdminDashboardComponent>;
    let dashboardService: { getSummary: ReturnType<typeof vi.fn> };
    let aiUsageService: { getSummary: ReturnType<typeof vi.fn> };

    beforeEach(async () => {
        dashboardService = { getSummary: vi.fn() };
        aiUsageService = { getSummary: vi.fn() };

        dashboardService.getSummary.mockReturnValue(
            of({
                totalUsers: 10,
                activeUsers: 8,
                premiumUsers: 3,
                deletedUsers: 2,
                recentUsers: [],
            }),
        );
        aiUsageService.getSummary.mockReturnValue(
            of({
                totalTokens: 1000,
                inputTokens: 600,
                outputTokens: 400,
                byDay: [],
                byOperation: [],
                byModel: [],
                byUser: [],
            }),
        );

        await TestBed.configureTestingModule({
            imports: [AdminDashboardComponent],
            providers: [
                { provide: AdminDashboardService, useValue: dashboardService },
                { provide: AdminAiUsageService, useValue: aiUsageService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminDashboardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load dashboard summary and ai usage on init', () => {
        expect(dashboardService.getSummary).toHaveBeenCalledTimes(1);
        expect(aiUsageService.getSummary).toHaveBeenCalledTimes(1);
        expect(component.summary()?.totalUsers).toBe(10);
        expect(component.aiUsage()?.totalTokens).toBe(1000);
        expect(component.isLoading()).toBe(false);
    });

    it('should reset summary and loading state on dashboard error', async () => {
        dashboardService.getSummary.mockReturnValueOnce(throwError(() => new Error('dashboard failed')));
        aiUsageService.getSummary.mockReturnValueOnce(of(null));

        fixture = TestBed.createComponent(AdminDashboardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        await fixture.whenStable();

        expect(component.summary()).toBeNull();
        expect(component.isLoading()).toBe(false);
    });
});
