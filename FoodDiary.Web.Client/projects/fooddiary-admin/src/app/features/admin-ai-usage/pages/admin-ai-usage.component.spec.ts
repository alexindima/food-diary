import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAiUsageService } from '../api/admin-ai-usage.service';
import { AdminAiUsageComponent } from './admin-ai-usage.component';

describe('AdminAiUsageComponent', () => {
    let component: AdminAiUsageComponent;
    let fixture: ComponentFixture<AdminAiUsageComponent>;
    let aiUsageService: { getSummary: ReturnType<typeof vi.fn> };

    beforeEach(async () => {
        aiUsageService = { getSummary: vi.fn() };
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
            imports: [AdminAiUsageComponent],
            providers: [{ provide: AdminAiUsageService, useValue: aiUsageService }],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminAiUsageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load ai usage on init', () => {
        expect(aiUsageService.getSummary).toHaveBeenCalledTimes(1);
        expect(component.usage()?.totalTokens).toBe(1000);
        expect(component.isLoading()).toBe(false);
    });

    it('should reset usage and loading state on error', async () => {
        aiUsageService.getSummary.mockReturnValueOnce(throwError(() => new Error('ai usage failed')));

        fixture = TestBed.createComponent(AdminAiUsageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        await fixture.whenStable();

        expect(component.usage()).toBeNull();
        expect(component.isLoading()).toBe(false);
    });
});
