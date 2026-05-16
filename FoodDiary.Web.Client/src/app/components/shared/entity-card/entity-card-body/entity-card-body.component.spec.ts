import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { EntityCardBodyComponent } from './entity-card-body.component';

async function setupEntityCardBodyAsync(): Promise<ComponentFixture<EntityCardBodyComponent>> {
    await TestBed.configureTestingModule({
        imports: [EntityCardBodyComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardBodyComponent);
    fixture.componentRef.setInput('showFavorite', true);
    fixture.componentRef.setInput('isFavorite', false);
    fixture.componentRef.setInput('favoriteLoading', false);
    fixture.componentRef.setInput('favoriteIcon', 'star_border');
    fixture.componentRef.setInput('title', 'Title');
    fixture.componentRef.setInput('nutrition', { proteins: 1, fats: 2, carbs: 3 });
    return fixture;
}

describe('EntityCardBodyComponent', () => {
    it('stops propagation and emits favorite toggle', async () => {
        const fixture = await setupEntityCardBodyAsync();
        const component = fixture.componentInstance;
        const stopPropagationSpy = vi.fn();
        const event = { stopPropagation: stopPropagationSpy } as unknown as Event;
        const favoriteSpy = vi.fn();
        component.favoriteToggle.subscribe(favoriteSpy);
        fixture.detectChanges();

        component.handleFavoriteToggle(event);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
        expect(favoriteSpy).toHaveBeenCalledOnce();
    });
});
