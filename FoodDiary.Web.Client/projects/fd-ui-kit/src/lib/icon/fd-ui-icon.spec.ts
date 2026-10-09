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
