import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { SkeletonCardComponent } from './skeleton-card.component';

describe('SkeletonCardComponent', () => {
    it('renders skeleton placeholders', async () => {
        await TestBed.configureTestingModule({
            imports: [SkeletonCardComponent],
        }).compileComponents();

        const fixture = TestBed.createComponent(SkeletonCardComponent);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('fd-skeleton')).toBeTruthy();
    });
});
