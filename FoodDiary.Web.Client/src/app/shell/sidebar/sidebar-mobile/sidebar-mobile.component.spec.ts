import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MOBILE_REPORT_ITEMS } from '../sidebar-lib/sidebar-navigation.config';
import { SidebarMobileComponent } from './sidebar-mobile.component';

const DAILY_CONSUMED = 850;
const DAILY_GOAL = 2000;
const DAILY_PROGRESS = 42.5;

describe('SidebarMobileComponent', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('derives open state and report links from selected mobile sheet', () => {
        const fixture = createComponent('reports');
        const host = fixture.nativeElement as HTMLElement;
        const sheet = host.querySelector('.sidebar-mobile__sheet');
        const links = host.querySelectorAll('.sidebar-mobile__sheet-link');

        expect(sheet?.getAttribute('aria-label')).toBe('SIDEBAR.REPORTS_AND_GOALS');
        expect(links.length).toBe(MOBILE_REPORT_ITEMS.length);
        expect(host.querySelectorAll('button.is-open').length).toBe(1);
    });

    it('emits the clicked mobile section trigger', () => {
        const fixture = createComponent(null);
        const component = fixture.componentInstance;
        const foodToggle = vi.fn();
        component.foodToggle.subscribe(foodToggle);

        const host = fixture.nativeElement as HTMLElement;
        const foodButton = host.querySelector('button[aria-haspopup="dialog"]');
        foodButton?.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(foodToggle).toHaveBeenCalledWith(foodButton);
    });
});

function createComponent(mobileSheet: 'food' | 'body' | 'reports' | 'user' | null): ComponentFixture<SidebarMobileComponent> {
    TestBed.configureTestingModule({
        imports: [SidebarMobileComponent, TranslateModule.forRoot()],
        providers: [provideRouter([])],
    });

    const fixture = TestBed.createComponent(SidebarMobileComponent);
    fixture.componentRef.setInput('isProgressVisible', true);
    fixture.componentRef.setInput('dailyConsumedKcalRounded', DAILY_CONSUMED);
    fixture.componentRef.setInput('dailyGoalKcalRounded', DAILY_GOAL);
    fixture.componentRef.setInput('dailyProgressPercent', DAILY_PROGRESS);
    fixture.componentRef.setInput('pendingRoute', null);
    fixture.componentRef.setInput('unreadNotificationCount', 0);
    fixture.componentRef.setInput('mobileSheet', mobileSheet);
    fixture.componentRef.setInput('isAdmin', false);
    fixture.detectChanges();

    return fixture;
}
