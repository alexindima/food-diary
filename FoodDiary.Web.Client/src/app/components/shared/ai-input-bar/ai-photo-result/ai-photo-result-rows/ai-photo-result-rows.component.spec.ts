import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AiPhotoResultRowsComponent } from './ai-photo-result-rows.component';

async function setupAiPhotoResultRowsAsync(): Promise<ComponentFixture<AiPhotoResultRowsComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoResultRowsComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoResultRowsComponent);
    fixture.componentRef.setInput('rows', [{ key: 'egg', displayName: 'Egg', amountLabel: '100 g' }]);
    return fixture;
}

describe('AiPhotoResultRowsComponent', () => {
    it('renders detected rows', async () => {
        const fixture = await setupAiPhotoResultRowsAsync();
        fixture.detectChanges();

        const text = (fixture.nativeElement as HTMLElement).textContent;
        expect(text).toContain('Egg');
        expect(text).toContain('100 g');
    });
});
