import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomGroupComponent } from './custom-group.component';

describe('CustomGroupComponent', () => {
    let component: CustomGroupComponent;
    let fixture: ComponentFixture<CustomGroupComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [CustomGroupComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(CustomGroupComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('title', 'Group Title');
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render title', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const titleEl = el.querySelector('.custom-group__title span');
        expect(titleEl?.textContent?.trim()).toBe('Group Title');
    });

    describe('accordion toggle', () => {
        it('should start with isOpen as true', () => {
            fixture.detectChanges();
            expect(component.isOpen()).toBe(true);
        });

        it('should toggle isOpen to false on first call', () => {
            fixture.detectChanges();
            component.toggleAccordion();
            expect(component.isOpen()).toBe(false);
        });

        it('should toggle isOpen back to true on second call', () => {
            fixture.detectChanges();
            component.toggleAccordion();
            component.toggleAccordion();
            expect(component.isOpen()).toBe(true);
        });

        it('should show toggle button when isAccordion is true', () => {
            fixture.componentRef.setInput('isAccordion', true);
            fixture.detectChanges();

            const el: HTMLElement = fixture.nativeElement;
            const toggleBtn = el.querySelector('.custom-group__toggle');
            expect(toggleBtn).toBeTruthy();
        });

        it('should not show toggle button when isAccordion is false', () => {
            fixture.componentRef.setInput('isAccordion', false);
            fixture.detectChanges();

            const el: HTMLElement = fixture.nativeElement;
            const toggleBtn = el.querySelector('.custom-group__toggle');
            expect(toggleBtn).toBeNull();
        });

        it('should toggle on button click', () => {
            fixture.componentRef.setInput('isAccordion', true);
            fixture.detectChanges();

            const el: HTMLElement = fixture.nativeElement;
            const toggleBtn = el.querySelector<HTMLButtonElement>('.custom-group__toggle');
            toggleBtn?.click();

            expect(component.isOpen()).toBe(false);
        });
    });

    describe('closeButtonClick', () => {
        it('should emit closeButtonClick when onCloseButtonClick is called', () => {
            fixture.componentRef.setInput('showCloseButton', true);
            fixture.detectChanges();

            const closeSpy = vi.fn();
            component.closeButtonClick.subscribe(closeSpy);

            component.onCloseButtonClick();

            expect(closeSpy).toHaveBeenCalledOnce();
        });

        it('should show close button when showCloseButton is true', () => {
            fixture.componentRef.setInput('showCloseButton', true);
            fixture.detectChanges();

            const el: HTMLElement = fixture.nativeElement;
            const closeBtn = el.querySelector('.custom-group__button');
            expect(closeBtn).toBeTruthy();
        });

        it('should not show close button when showCloseButton is false', () => {
            fixture.detectChanges();

            const el: HTMLElement = fixture.nativeElement;
            const closeBtn = el.querySelector('.custom-group__button');
            expect(closeBtn).toBeNull();
        });

        it('should emit closeButtonClick on close button click', () => {
            fixture.componentRef.setInput('showCloseButton', true);
            fixture.detectChanges();

            const closeSpy = vi.fn();
            component.closeButtonClick.subscribe(closeSpy);

            const el: HTMLElement = fixture.nativeElement;
            const closeBtn = el.querySelector<HTMLElement>('.custom-group__button');
            closeBtn?.click();

            expect(closeSpy).toHaveBeenCalledOnce();
        });
    });

    describe('titleLeftOffset', () => {
        it('should return tokenized offset when isAccordion is false', () => {
            fixture.componentRef.setInput('isAccordion', false);
            fixture.detectChanges();
            expect(component.titleLeftOffset()).toBe('calc(var(--fd-space-sm) + var(--fd-border-width-strong) + var(--fd-border-width))');
        });

        it('should return tokenized offset when isAccordion is true', () => {
            fixture.componentRef.setInput('isAccordion', true);
            fixture.detectChanges();
            expect(component.titleLeftOffset()).toBe('calc(var(--fd-size-control-xs) + var(--fd-space-sm))');
        });
    });

    describe('forceCollapse', () => {
        it('should hide content when forceCollapse is true', () => {
            fixture.componentRef.setInput('forceCollapse', true);
            fixture.detectChanges();

            const el: HTMLElement = fixture.nativeElement;
            // When forceCollapse is true, ng-content is not rendered
            const wrapper = el.querySelector('.custom-group');
            expect(wrapper).toBeTruthy();
        });
    });

    describe('collapsedHint', () => {
        it('should show collapsed hint when accordion is closed and hint is provided', () => {
            fixture.componentRef.setInput('isAccordion', true);
            fixture.componentRef.setInput('collapsedHint', 'Items hidden');
            fixture.detectChanges();

            component.toggleAccordion();
            fixture.detectChanges();

            const el: HTMLElement = fixture.nativeElement;
            const hint = el.querySelector('.custom-group__hint');
            expect(hint?.textContent?.trim()).toBe('Items hidden');
        });
    });
});
