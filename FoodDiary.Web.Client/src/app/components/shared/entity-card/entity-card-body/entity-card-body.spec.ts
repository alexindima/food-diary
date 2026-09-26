import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { EntityCardBodyComponent } from './entity-card-body';

const CALORIES = 100;

async function setupEntityCardBodyAsync(): Promise<ComponentFixture<EntityCardBodyComponent>> {
    await TestBed.configureTestingModule({
        imports: [EntityCardBodyComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardBodyComponent);
    fixture.componentRef.setInput('title', 'Title');
    fixture.componentRef.setInput('nutrition', { proteins: 1, fats: 2, carbs: 3, fiber: 4, alcohol: 0 });
    fixture.componentRef.setInput('calories', CALORIES);
    return fixture;
}

describe('EntityCardBodyComponent', () => {
    it('places a focusable nutrition warning beside unknown calories without opening the card', async () => {
        const fixture = await setupEntityCardBodyAsync();
        fixture.componentRef.setInput('calories', null);
        fixture.componentRef.setInput('nutritionNote', 'Partial nutrition');
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const warning = element.querySelector<HTMLButtonElement>('.entity-card__calories button');
        expect(warning?.getAttribute('aria-label')).toBe('Partial nutrition');
        expect(element.querySelector('.entity-card__calories-value')?.textContent).toBe('—');
        let bubbled = false;
        element.addEventListener('click', () => {
            bubbled = true;
        });
        warning?.click();
        expect(bubbled).toBe(false);
    });

    it('does not show a warning for complete nutrition', async () => {
        const fixture = await setupEntityCardBodyAsync();
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('.entity-card__nutrition-warning')).toBeNull();
    });

    it('should create', async () => {
        const fixture = await setupEntityCardBodyAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        expect(component).toBeTruthy();
    });

    it('should render comment when provided', async () => {
        const fixture = await setupEntityCardBodyAsync();
        fixture.componentRef.setInput('comment', 'Useful card comment');
        fixture.detectChanges();

        const el = fixture.nativeElement as HTMLElement;
        const commentEl = el.querySelector('.entity-card__comment');

        expect(commentEl).not.toBeNull();
        expect(commentEl?.textContent.trim()).toBe('Useful card comment');
    });
});
