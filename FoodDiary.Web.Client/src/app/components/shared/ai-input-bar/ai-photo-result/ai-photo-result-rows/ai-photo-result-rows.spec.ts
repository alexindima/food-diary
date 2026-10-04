import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { AiPhotoResultRowsComponent } from './ai-photo-result-rows';

async function setupAiPhotoResultRowsAsync(): Promise<ComponentFixture<AiPhotoResultRowsComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoResultRowsComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoResultRowsComponent);
    fixture.componentRef.setInput('rows', [{ key: 'egg', annotationId: 'egg-0', displayName: 'Egg', amountLabel: '100 g', calories: 155 }]);
    return fixture;
}

describe('AiPhotoResultRowsComponent', () => {
    it('reacts to the interface language when formatting row calories', async () => {
        const fixture = await setupAiPhotoResultRowsAsync();
        fixture.componentRef.setInput('rows', [
            { key: 'egg', annotationId: 'egg-0', displayName: 'Egg', amountLabel: '100 g', calories: 1056.4 },
        ]);
        const translateService = TestBed.inject(TranslateService);
        const element = fixture.nativeElement as HTMLElement;
        translateService.use('ru');
        fixture.detectChanges();
        expect(element.querySelector('.ai-photo-result__item-calories')?.textContent.replaceAll('\u00A0', ' ')).toContain('1 056');
        translateService.use('en');
        fixture.detectChanges();
        expect(element.querySelector('.ai-photo-result__item-calories')?.textContent).toContain('1,056');
    });

    it('renders detected rows', async () => {
        const fixture = await setupAiPhotoResultRowsAsync();
        fixture.detectChanges();

        const text = (fixture.nativeElement as HTMLElement).textContent;
        expect(text).toContain('Egg');
        expect(text).toContain('100 g');
        expect(text).toContain('155');
    });

    it('emits the selected annotation', async () => {
        const fixture = await setupAiPhotoResultRowsAsync();
        let selectedId: string | null = null;
        fixture.componentInstance.rowSelected.subscribe(id => {
            selectedId = id;
        });
        fixture.detectChanges();

        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button')?.click();

        expect(selectedId).toBe('egg-0');
    });
});
