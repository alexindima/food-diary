import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ManageHeaderComponent } from './manage-header.component';

describe('ManageHeaderComponent', () => {
    let component: ManageHeaderComponent;
    let fixture: ComponentFixture<ManageHeaderComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ManageHeaderComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ManageHeaderComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('title', 'Manage Title');
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render title', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const titleEl = el.querySelector('.fd-manage-header__title');
        expect(titleEl?.textContent?.trim()).toBe('Manage Title');
    });

    it('should render subtitle when provided', () => {
        fixture.componentRef.setInput('subtitle', 'Some subtitle');
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const subtitleEl = el.querySelector('.fd-manage-header__subtitle');
        expect(subtitleEl).toBeTruthy();
        expect(subtitleEl?.textContent?.trim()).toBe('Some subtitle');
    });

    it('should not render subtitle when null', () => {
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const subtitleEl = el.querySelector('.fd-manage-header__subtitle');
        expect(subtitleEl).toBeNull();
    });

    it('should emit back on back button click', () => {
        fixture.detectChanges();

        const backSpy = vi.fn();
        component.back.subscribe(backSpy);

        const el: HTMLElement = fixture.nativeElement;
        const backBtn = el.querySelector<HTMLElement>('.fd-manage-header__back');
        backBtn?.click();

        expect(backSpy).toHaveBeenCalledOnce();
    });

    it('should default mobileBackOnly to true', () => {
        fixture.detectChanges();
        expect(component.mobileBackOnly()).toBe(true);
    });

    it('should default backAriaLabel to "Back"', () => {
        fixture.detectChanges();
        expect(component.backAriaLabel()).toBe('Back');
    });
});
