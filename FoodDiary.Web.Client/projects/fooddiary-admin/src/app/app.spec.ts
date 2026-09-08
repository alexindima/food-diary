import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { AppComponent } from './app';

const ADMIN_ROUTE_COUNT = 17;

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
        const routeSelector = (fixture.nativeElement as HTMLElement).querySelector<HTMLSelectElement>('#admin-mobile-route');

        expect(routeSelector?.options).toHaveLength(ADMIN_ROUTE_COUNT);
        expect(routeSelector?.value).toBe('/');
    });
});
