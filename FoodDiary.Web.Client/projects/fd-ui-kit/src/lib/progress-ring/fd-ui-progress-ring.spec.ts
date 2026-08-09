import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiProgressRingComponent } from './fd-ui-progress-ring';

const HALF = 50;
const OVER_MAX = 150;

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
});
