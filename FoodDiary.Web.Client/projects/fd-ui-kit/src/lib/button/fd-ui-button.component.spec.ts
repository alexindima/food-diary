import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { FdUiButtonComponent } from './fd-ui-button.component';

describe('FdUiButtonComponent', () => {
    let component: FdUiButtonComponent;
    let fixture: ComponentFixture<FdUiButtonComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const button = (): HTMLButtonElement => {
        const element = host().querySelector<HTMLButtonElement>('button');
        if (element === null) {
            throw new Error('Expected button to exist.');
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiButtonComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiButtonComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have default classes (primary, solid, md)', () => {
        const classes = component.classes();
        expect(classes).toContain('fd-ui-button');
        expect(classes).toContain('fd-ui-button--primary');
        expect(classes).toContain('fd-ui-button--solid');
        expect(classes).toContain('fd-ui-button--appearance-default');
        expect(classes).toContain('fd-ui-button--size-md');
        expect(classes).toContain('fd-ui-button--icon-md');
    });

    it('should update classes when appearance changes', () => {
        fixture.componentRef.setInput('appearance', 'toolbar');
        fixture.detectChanges();

        const classes = component.classes();
        expect(classes).toContain('fd-ui-button--appearance-toolbar');
        expect(classes).not.toContain('fd-ui-button--appearance-default');
    });

    it('should update classes when variant changes', () => {
        fixture.componentRef.setInput('variant', 'danger');
        fixture.detectChanges();

        const classes = component.classes();
        expect(classes).toContain('fd-ui-button--danger');
        expect(classes).not.toContain('fd-ui-button--primary');
    });

    it('should normalize fill to text for ghost variant', () => {
        fixture.componentRef.setInput('variant', 'ghost');
        fixture.detectChanges();

        const classes = component.classes();
        expect(classes).toContain('fd-ui-button--text');
        expect(classes).not.toContain('fd-ui-button--solid');
    });

    it('should normalize fill to outline for outline variant', () => {
        fixture.componentRef.setInput('variant', 'outline');
        fixture.detectChanges();

        const classes = component.classes();
        expect(classes).toContain('fd-ui-button--outline');
        expect(classes).not.toContain('fd-ui-button--solid');
    });

    it('should normalize fill ghost to text', () => {
        fixture.componentRef.setInput('fill', 'ghost');
        fixture.detectChanges();

        const classes = component.classes();
        expect(classes).toContain('fd-ui-button--text');
        expect(classes).not.toContain('fd-ui-button--ghost');
    });

    it('should add full-width class when fullWidth is true', () => {
        fixture.componentRef.setInput('fullWidth', true);
        fixture.detectChanges();

        const classes = component.classes();
        expect(classes).toContain('fd-ui-button--full-width');
    });

    it('should set button type attribute', () => {
        fixture.componentRef.setInput('type', 'submit');
        fixture.detectChanges();

        expect(button().getAttribute('type')).toBe('submit');
    });

    it('should set disabled attribute', () => {
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(button().disabled).toBe(true);
    });

    it('should disable button and show spinner when loading', () => {
        fixture.componentRef.setInput('loading', true);
        fixture.detectChanges();

        const spinner = fixture.debugElement.query(By.css('.fd-ui-button__spinner'));

        expect(button().disabled).toBe(true);
        expect(button().getAttribute('aria-busy')).toBe('true');
        expect(spinner).toBeTruthy();
        expect(component.classes()).toContain('fd-ui-button--loading');
    });

    it('should keep spinner positioned inside the button', () => {
        fixture.componentRef.setInput('loading', true);
        fixture.detectChanges();

        expect(getComputedStyle(button()).position).toBe('relative');
    });

    it('should set aria-label attribute', () => {
        fixture.componentRef.setInput('ariaLabel', 'Close dialog');
        fixture.detectChanges();

        expect(button().getAttribute('aria-label')).toBe('Close dialog');
    });

    it('should render icon when provided', () => {
        fixture.componentRef.setInput('icon', 'add');
        fixture.detectChanges();

        const icon = fixture.debugElement.query(By.directive(FdUiIconComponent));
        expect(icon).toBeTruthy();
        expect((icon.componentInstance as FdUiIconComponent).name()).toBe('add');
    });
});
