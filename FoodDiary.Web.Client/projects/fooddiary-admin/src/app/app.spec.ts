import { DOCUMENT } from '@angular/common';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { AppComponent } from './app';

const ADMIN_ROUTE_COUNT = 21;

describe('AppComponent', () => {
    let component: AppComponent;
    let fixture: ComponentFixture<AppComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppComponent],
            providers: [provideRouter([]), provideTranslateService()],
        }).compileComponents();

        fixture = TestBed.createComponent(AppComponent);
        component = fixture.componentInstance;
        await fixture.whenStable();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('renders the current route as the page heading', () => {
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toContain('ADMIN_NAV.DASHBOARD');
    });

    it('provides every admin section through the mobile route selector', () => {
        fixture.detectChanges();
        const routeSelector = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button#admin-mobile-route');

        expect(routeSelector?.textContent).toContain('ADMIN_NAV.DASHBOARD');
        routeSelector?.click();
        fixture.detectChanges();
        const options = TestBed.inject(DOCUMENT).querySelectorAll('[role=option]');
        expect(options).toHaveLength(ADMIN_ROUTE_COUNT);
        expect(Array.from(options).some(option => option.textContent.includes('ADMIN_FEEDBACK.TITLE'))).toBe(true);
    });
});
