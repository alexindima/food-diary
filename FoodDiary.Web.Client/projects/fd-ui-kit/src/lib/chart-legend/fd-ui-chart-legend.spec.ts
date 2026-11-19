import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiChartLegendComponent } from './fd-ui-chart-legend';

describe('FdUiChartLegendComponent', () => {
    it('keeps its items mounted while hidden so switching metrics preserves the reserved legend space', () => {
        const fixture = TestBed.createComponent(FdUiChartLegendComponent);
        const host = fixture.nativeElement as HTMLElement;
        fixture.componentRef.setInput('items', [{ label: 'Energy', color: 'var(--fd-color-blue-500)' }]);
        fixture.detectChanges();

        const item = host.querySelector('.fd-ui-chart-legend__item');
        expect(item?.textContent.trim()).toBe('Energy');
        expect(host.hasAttribute('aria-hidden')).toBe(false);

        fixture.componentRef.setInput('visible', false);
        fixture.detectChanges();
        expect(host.getAttribute('aria-hidden')).toBe('true');
        expect(host.classList.contains('fd-ui-chart-legend--hidden')).toBe(true);
        expect(host.querySelector('.fd-ui-chart-legend__item')).toBe(item);

        fixture.componentRef.setInput('visible', true);
        fixture.detectChanges();
        expect(host.hasAttribute('aria-hidden')).toBe(false);
        expect(host.classList.contains('fd-ui-chart-legend--hidden')).toBe(false);
        expect(host.querySelector('.fd-ui-chart-legend__item')).toBe(item);
    });
});
