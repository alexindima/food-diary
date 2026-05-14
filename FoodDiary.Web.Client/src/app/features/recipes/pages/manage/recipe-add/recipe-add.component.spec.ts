import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { RecipeAddComponent } from './recipe-add.component';

describe('RecipeAddComponent', () => {
    it('creates add page shell', () => {
        TestBed.configureTestingModule({
            imports: [RecipeAddComponent],
        });
        TestBed.overrideComponent(RecipeAddComponent, {
            set: { template: '' },
        });

        const fixture = TestBed.createComponent(RecipeAddComponent);

        expect(fixture.componentInstance).toBeTruthy();
    });
});
