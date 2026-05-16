import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { SkeletonComponent } from './skeleton.component';

const CIRCLE_SIZE = '40px';
const RECT_WIDTH = '120px';
const CUSTOM_RADIUS = '8px';

describe('SkeletonComponent', () => {
    it('uses text defaults', () => {
        const fixture = createComponent();

        expect(fixture.componentInstance.styles()).toEqual({
            width: '100%',
            height: 'var(--fd-text-body-size)',
            'border-radius': 'var(--fd-radius-xs)',
        });
    });

    it('uses equal width and height for circle default width', () => {
        const fixture = createComponent({ variant: 'circle', height: CIRCLE_SIZE });

        expect(fixture.componentInstance.styles()).toEqual({
            width: CIRCLE_SIZE,
            height: CIRCLE_SIZE,
            'border-radius': '50%',
        });
    });

    it('uses rect defaults and custom radius', () => {
        const fixture = createComponent({ variant: 'rect', width: RECT_WIDTH, borderRadius: CUSTOM_RADIUS });

        expect(fixture.componentInstance.styles()).toEqual({
            width: RECT_WIDTH,
            height: 'calc((var(--fd-size-control-xl) * 2) + var(--fd-space-xs))',
            'border-radius': CUSTOM_RADIUS,
        });
    });
});

function createComponent(
    inputs: Partial<{ variant: string; width: string; height: string; borderRadius: string }> = {},
): ComponentFixture<SkeletonComponent> {
    TestBed.configureTestingModule({
        imports: [SkeletonComponent],
    });

    const fixture = TestBed.createComponent(SkeletonComponent);
    for (const [key, value] of Object.entries(inputs)) {
        fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
}
