import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, type MockInstance, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { PageHeaderComponent } from './page-header';

describe('PageHeaderComponent', () => {
    let component: PageHeaderComponent;
    let fixture: ComponentFixture<PageHeaderComponent>;
    let backSpy: MockInstance;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [PageHeaderComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        fixture = TestBed.createComponent(PageHeaderComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('title', 'Page Title');
        backSpy = vi.spyOn(component.back, 'emit');
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render title', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const titleEl = el.querySelector('.fd-page-title');
        expect(titleEl?.textContent.trim()).toBe('Page Title');
    });

    it('should render subtitle when provided', () => {
        fixture.componentRef.setInput('subtitle', 'A subtitle');
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const subtitleEl = el.querySelector('.fd-page-subtitle');
        expect(subtitleEl).toBeTruthy();
        expect(subtitleEl?.textContent.trim()).toBe('A subtitle');
    });

    it('should not render subtitle when not provided', () => {
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const subtitleEl = el.querySelector('.fd-page-subtitle');
        expect(subtitleEl).toBeNull();
    });

    it('should render back button when enabled', () => {
        fixture.componentRef.setInput('backVisible', true);
        fixture.componentRef.setInput('backAriaLabel', 'Go back');
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const backEl = el.querySelector('.fd-page-header__back');
        const backButtonEl = el.querySelector('.fd-page-header__back button');
        expect(backEl).toBeTruthy();
        expect(backButtonEl?.getAttribute('aria-label')).toBe('Go back');
    });

    it('should emit back event when back button is clicked', () => {
        fixture.componentRef.setInput('backVisible', true);
        fixture.detectChanges();

        const backDebugEl = fixture.debugElement.query(By.css('.fd-page-header__back button'));
        (backDebugEl.nativeElement as HTMLButtonElement).click();

        expect(backSpy).toHaveBeenCalledOnce();
    });
});
