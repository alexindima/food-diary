import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PageHeaderComponent } from './page-header.component';

describe('PageHeaderComponent', () => {
    let component: PageHeaderComponent;
    let fixture: ComponentFixture<PageHeaderComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [PageHeaderComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(PageHeaderComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('title', 'Page Title');
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render title', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const titleEl = el.querySelector('.fd-page-title');
        expect(titleEl?.textContent?.trim()).toBe('Page Title');
    });

    it('should render subtitle when provided', () => {
        fixture.componentRef.setInput('subtitle', 'A subtitle');
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const subtitleEl = el.querySelector('.fd-page-subtitle');
        expect(subtitleEl).toBeTruthy();
        expect(subtitleEl?.textContent?.trim()).toBe('A subtitle');
    });

    it('should not render subtitle when not provided', () => {
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const subtitleEl = el.querySelector('.fd-page-subtitle');
        expect(subtitleEl).toBeNull();
    });

    it('should default stickyOnMobile to true', () => {
        fixture.detectChanges();
        expect(component.stickyOnMobile()).toBe(true);
    });

    it('should apply mobile-static host class when stickyOnMobile is false', () => {
        fixture.componentRef.setInput('stickyOnMobile', false);
        fixture.detectChanges();

        const hostEl: HTMLElement = fixture.nativeElement;
        expect(hostEl.classList.contains('fd-page-header--mobile-static')).toBe(true);
    });

    it('should not apply mobile-static host class when stickyOnMobile is true', () => {
        fixture.detectChanges();

        const hostEl: HTMLElement = fixture.nativeElement;
        expect(hostEl.classList.contains('fd-page-header--mobile-static')).toBe(false);
    });
});
