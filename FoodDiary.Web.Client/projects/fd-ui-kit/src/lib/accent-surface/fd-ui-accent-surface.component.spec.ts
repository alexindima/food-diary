import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiAccentSurfaceComponent } from './fd-ui-accent-surface.component';

describe('FdUiAccentSurfaceComponent', () => {
    let fixture: ComponentFixture<FdUiAccentSurfaceComponent>;
    let hostEl: HTMLElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiAccentSurfaceComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiAccentSurfaceComponent);
        hostEl = fixture.nativeElement;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });

    it('should apply accent color as CSS variable', () => {
        fixture.componentRef.setInput('accentColor', '#ff0000');
        fixture.detectChanges();

        const style = hostEl.style.getPropertyValue('--fd-accent-color');
        expect(style).toBe('#ff0000');
    });

    it('should set side class', () => {
        // Default side is 'top'
        expect(hostEl.classList).toContain('fd-ui-accent-surface--side-top');

        fixture.componentRef.setInput('accentSide', 'right');
        fixture.detectChanges();
        expect(hostEl.classList).toContain('fd-ui-accent-surface--side-right');

        fixture.componentRef.setInput('accentSide', 'bottom');
        fixture.detectChanges();
        expect(hostEl.classList).toContain('fd-ui-accent-surface--side-bottom');

        fixture.componentRef.setInput('accentSide', 'left');
        fixture.detectChanges();
        expect(hostEl.classList).toContain('fd-ui-accent-surface--side-left');
    });

    it('should add active class when active', () => {
        expect(hostEl.classList).not.toContain('fd-ui-accent-surface--active');

        fixture.componentRef.setInput('active', true);
        fixture.detectChanges();
        expect(hostEl.classList).toContain('fd-ui-accent-surface--active');
    });

    it('should add tinted class when tinted', () => {
        expect(hostEl.classList).not.toContain('fd-ui-accent-surface--tinted');

        fixture.componentRef.setInput('tinted', true);
        fixture.detectChanges();
        expect(hostEl.classList).toContain('fd-ui-accent-surface--tinted');
    });
});
