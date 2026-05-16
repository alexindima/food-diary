import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { MediaCardComponent } from './media-card.component';

describe('MediaCardComponent', () => {
    it('creates media card shell', async () => {
        await TestBed.configureTestingModule({
            imports: [MediaCardComponent],
        }).compileComponents();

        const fixture = TestBed.createComponent(MediaCardComponent);
        fixture.detectChanges();

        expect(fixture.componentInstance).toBeTruthy();
    });
});
