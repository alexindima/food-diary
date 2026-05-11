import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAiUsageService } from '../api/admin-ai-usage.service';
import { AdminAiUsageComponent } from './admin-ai-usage.component';

const TOTAL_TOKENS = 1000;
const INPUT_TOKENS = 600;
const OUTPUT_TOKENS = 400;

describe('AdminAiUsageComponent', () => {
    let component: AdminAiUsageComponent;
    let fixture: ComponentFixture<AdminAiUsageComponent>;
    let aiUsageService: { getSummary: ReturnType<typeof vi.fn> };

    beforeEach(async () => {
        aiUsageService = { getSummary: vi.fn() };
        aiUsageService.getSummary.mockReturnValue(
            of({
                totalTokens: TOTAL_TOKENS,
                inputTokens: INPUT_TOKENS,
                outputTokens: OUTPUT_TOKENS,
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
        expect(component.usage()?.totalTokens).toBe(TOTAL_TOKENS);
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
