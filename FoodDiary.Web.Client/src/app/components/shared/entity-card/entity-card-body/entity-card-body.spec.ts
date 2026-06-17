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
    it('should create', async () => {
        const fixture = await setupEntityCardBodyAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        expect(component).toBeTruthy();
    });
});
