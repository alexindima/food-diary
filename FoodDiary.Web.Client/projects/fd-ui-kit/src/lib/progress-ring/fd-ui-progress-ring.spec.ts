import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiProgressRingComponent } from './fd-ui-progress-ring';

const HALF = 50;
const OVER_MAX = 150;
const CUSTOM_MAX = 200;

describe('FdUiProgressRingComponent', () => {
    let fixture: ComponentFixture<FdUiProgressRingComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [FdUiProgressRingComponent] });
        fixture = TestBed.createComponent(FdUiProgressRingComponent);
        fixture.componentRef.setInput('value', HALF);
        fixture.componentRef.setInput('ariaLabel', 'Half complete');
        fixture.detectChanges();
    });

    it('renders an accessible normalized progress ring', () => {
        const element = fixture.nativeElement as HTMLElement;
        const ring = element.querySelector<HTMLElement>('.fd-ui-progress-ring');
        const progress = element.querySelector<SVGCircleElement>('.fd-ui-progress-ring__value');

        expect(ring?.getAttribute('role')).toBe('progressbar');
        expect(ring?.getAttribute('aria-label')).toBe('Half complete');
        expect(ring?.getAttribute('aria-valuenow')).toBe(HALF.toString());
        expect(progress?.getAttribute('stroke-dasharray')).toBe('50 100');
    });

    it('clamps values outside the supported range', () => {
        fixture.componentRef.setInput('value', OVER_MAX);
        fixture.detectChanges();

        const progress = (fixture.nativeElement as HTMLElement).querySelector<SVGCircleElement>('.fd-ui-progress-ring__value');
        expect(progress?.getAttribute('stroke-dasharray')).toBe('100 100');
        expect((fixture.nativeElement as HTMLElement).querySelector('.fd-ui-progress-ring')?.getAttribute('aria-valuenow')).toBe('100');
    });

    it('adds the progress indicator to keyboard navigation when requested and describes the custom range', () => {
        fixture.componentRef.setInput('max', CUSTOM_MAX);
        fixture.componentRef.setInput('focusable', true);
        fixture.componentRef.setInput('ariaDescribedBy', 'upload-status');
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        const ring = element.querySelector<HTMLElement>('[role="progressbar"]');
        expect(ring?.getAttribute('tabindex')).toBe('0');
        expect(ring?.getAttribute('aria-describedby')).toBe('upload-status');
        expect(ring?.getAttribute('aria-valuemax')).toBe(CUSTOM_MAX.toString());
        expect(ring?.getAttribute('aria-valuenow')).toBe(HALF.toString());
        expect(element.querySelector('.fd-ui-progress-ring__value')?.getAttribute('stroke-dasharray')).toBe('25 100');
    });

    it.each([
        { name: 'an unavailable maximum', max: Number.NaN, value: HALF },
        { name: 'a zero maximum', max: 0, value: HALF },
        { name: 'an unavailable value', max: CUSTOM_MAX, value: Number.NaN },
        { name: 'a negative value', max: CUSTOM_MAX, value: -HALF },
    ])('renders empty progress for $name', ({ max, value }) => {
        fixture.componentRef.setInput('max', max);
        fixture.componentRef.setInput('value', value);
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('[role="progressbar"]')?.getAttribute('aria-valuenow')).toBe('0');
        expect(element.querySelector('.fd-ui-progress-ring__value')?.getAttribute('stroke-dasharray')).toBe('0 100');
    });
});
