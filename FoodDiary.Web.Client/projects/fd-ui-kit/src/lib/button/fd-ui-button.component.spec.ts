import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { MatIconModule } from '@angular/material/icon';
import { FdUiButtonComponent } from './fd-ui-button.component';

describe('FdUiButtonComponent', () => {
    let component: FdUiButtonComponent;
    let fixture: ComponentFixture<FdUiButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiButtonComponent, MatIconModule],
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

        const button = fixture.debugElement.query(By.css('button'));
        expect(button.nativeElement.getAttribute('type')).toBe('submit');
    });

    it('should set disabled attribute', () => {
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        const button = fixture.debugElement.query(By.css('button'));
        expect(button.nativeElement.disabled).toBe(true);
    });

    it('should set aria-label attribute', () => {
        fixture.componentRef.setInput('ariaLabel', 'Close dialog');
        fixture.detectChanges();

        const button = fixture.debugElement.query(By.css('button'));
        expect(button.nativeElement.getAttribute('aria-label')).toBe('Close dialog');
    });

    it('should render icon when provided', () => {
        fixture.componentRef.setInput('icon', 'add');
        fixture.detectChanges();

        const icon = fixture.debugElement.query(By.css('mat-icon'));
        expect(icon).toBeTruthy();
        expect(icon.nativeElement.textContent.trim()).toBe('add');
    });
});
