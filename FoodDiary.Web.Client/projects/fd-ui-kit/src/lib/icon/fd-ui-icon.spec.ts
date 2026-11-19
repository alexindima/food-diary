import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiIconComponent } from './fd-ui-icon';

describe('FdUiIconComponent font selection', () => {
    it('uses the small font for known icons', () => {
        // Arrange
        const fixture = TestBed.createComponent(FdUiIconComponent);
        fixture.componentRef.setInput('name', 'menu');
        // Act
        fixture.detectChanges();
        // Assert
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.fd-material-icons')).not.toBeNull();
    });

    it('keeps the full font for new or server-provided icon names', () => {
        // Arrange
        const fixture = TestBed.createComponent(FdUiIconComponent);
        fixture.componentRef.setInput('name', 'server_provided_icon');
        // Act
        fixture.detectChanges();
        // Assert
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.material-icons')).not.toBeNull();
    });

    it('preserves an explicitly selected font set', () => {
        // Arrange
        const fixture = TestBed.createComponent(FdUiIconComponent);
        fixture.componentRef.setInput('name', 'menu');
        fixture.componentRef.setInput('fontSet', ' custom-icons ');
        // Act
        fixture.detectChanges();
        // Assert
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.custom-icons')).not.toBeNull();
        expect(host.querySelector('.fd-material-icons')).toBeNull();
    });
});

describe('FdUiIconComponent host semantics', () => {
    it('uses tokens or pixels for an explicit size and removes the size override on reset', () => {
        const pixelSize = 28;
        const fixture = TestBed.createComponent(FdUiIconComponent);
        const host = fixture.nativeElement as HTMLElement;
        fixture.componentRef.setInput('name', 'menu');
        fixture.componentRef.setInput('size', 'sm');
        fixture.detectChanges();
        expect(host.style.getPropertyValue('--fd-icon-size')).toBe('var(--fd-size-icon-sm)');

        fixture.componentRef.setInput('size', pixelSize);
        fixture.detectChanges();
        expect(host.style.getPropertyValue('--fd-icon-size')).toBe('28px');

        fixture.componentRef.setInput('size', null);
        fixture.detectChanges();
        expect(host.style.getPropertyValue('--fd-icon-size')).toBe('');
    });

    it('uses the requested accessible name with the glyph name as fallback, and hides decorative icons', () => {
        const fixture = TestBed.createComponent(FdUiIconComponent);
        const host = fixture.nativeElement as HTMLElement;
        fixture.componentRef.setInput('name', 'menu');
        fixture.componentRef.setInput('decorative', false);
        fixture.detectChanges();
        expect(host.getAttribute('role')).toBe('img');
        expect(host.getAttribute('aria-label')).toBe('menu');
        expect(host.hasAttribute('aria-hidden')).toBe(false);

        fixture.componentRef.setInput('ariaLabel', 'Navigation menu');
        fixture.detectChanges();
        expect(host.getAttribute('aria-label')).toBe('Navigation menu');

        fixture.componentRef.setInput('decorative', true);
        fixture.detectChanges();
        expect(host.getAttribute('aria-hidden')).toBe('true');
        expect(host.hasAttribute('aria-label')).toBe(false);
        expect(host.hasAttribute('role')).toBe(false);
    });
});
