import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import { AdminAiPromptWorkbenchComponent } from './admin-ai-prompt-workbench';

function workbenchButton(root: HTMLElement, key: string): HTMLButtonElement {
    const result = Array.from(root.querySelectorAll('button')).find(button => button.textContent.trim() === key);
    if (result === undefined) {
        throw new Error(`Missing workbench button ${key}`);
    }
    return result;
}

function choosePhoto(root: HTMLElement, file: File | null): void {
    const input = root.querySelector<HTMLInputElement>('input[type="file"]');
    if (input === null) {
        throw new Error('Missing workbench photo input');
    }
    Object.defineProperty(input, 'files', { configurable: true, value: { item: (): File | null => file } });
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

describe('AdminAiPromptWorkbenchComponent rendered interactions', () => {
    const api = {
        preview: vi.fn<AdminAiPromptsFacade['preview']>(),
        test: vi.fn<AdminAiPromptsFacade['test']>(),
        uploadImage: vi.fn<AdminAiPromptsFacade['uploadImage']>(),
    };
    let fixture: ComponentFixture<AdminAiPromptWorkbenchComponent>;
    let root: HTMLElement;

    beforeEach(async () => {
        vi.resetAllMocks();
        api.preview.mockReturnValue(of({ text: 'Preview with selected photo' }));
        api.test.mockReturnValue(of({ text: '{"items":[]}' }));
        await TestBed.configureTestingModule({
            imports: [AdminAiPromptWorkbenchComponent],
            providers: [...provideTranslateTesting(), { provide: AdminAiPromptsFacade, useValue: api }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminAiPromptWorkbenchComponent);
        fixture.componentRef.setInput('scenarioKey', 'vision');
        fixture.componentRef.setInput('locale', 'en');
        fixture.componentRef.setInput('promptText', 'Recognize food');
        fixture.detectChanges();
        root = fixture.nativeElement as HTMLElement;
    });

    it('drops a preview that was started before leaving and reopening the same scenario', async () => {
        const pending = new Subject<{ text: string }>();
        const verified = vi.fn<(text: string) => void>();
        fixture.componentInstance.verified.subscribe(verified);
        api.preview.mockReturnValueOnce(pending);
        workbenchButton(root, 'ADMIN_PROMPTS.PREVIEW').click();
        expect(api.preview).toHaveBeenCalledTimes(1);

        fixture.componentRef.setInput('scenarioKey', 'nutrition');
        fixture.detectChanges();
        fixture.componentRef.setInput('scenarioKey', 'vision');
        fixture.detectChanges();
        pending.next({ text: 'Preview from the previous scenario visit' });
        pending.complete();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(root.textContent).not.toContain('Preview from the previous scenario visit');
        expect(verified).not.toHaveBeenCalled();
    });

    it('drops a photo uploaded before leaving and reopening the same locale', async () => {
        const pending = new Subject<{ assetId: string }>();
        const photo = new File(['photo'], 'previous-visit.png', { type: 'image/png' });
        api.uploadImage.mockReturnValueOnce(pending);
        choosePhoto(root, photo);
        expect(api.uploadImage).toHaveBeenCalledExactlyOnceWith(photo);

        fixture.componentRef.setInput('locale', 'ru');
        fixture.detectChanges();
        fixture.componentRef.setInput('locale', 'en');
        fixture.detectChanges();
        pending.next({ assetId: 'previous-visit' });
        pending.complete();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(root.textContent).not.toContain(photo.name);
        workbenchButton(root, 'ADMIN_PROMPTS.PREVIEW').click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(api.preview.mock.lastCall?.[0].imageAssetId).toBeUndefined();
        expect(workbenchButton(root, 'ADMIN_PROMPTS.TEST').disabled).toBe(true);
    });
});

describe('AdminAiPromptWorkbenchComponent', () => {
    const api = { preview: vi.fn(), test: vi.fn(), uploadImage: vi.fn() };
    beforeEach(async () => {
        vi.clearAllMocks();
        api.preview.mockReturnValue(of({ text: 'Preview' }));
        api.test.mockReturnValue(of({ text: '{"items":[]}' }));
        await TestBed.configureTestingModule({
            imports: [AdminAiPromptWorkbenchComponent],
            providers: [...provideTranslateTesting(), { provide: AdminAiPromptsFacade, useValue: api }],
        }).compileComponents();
    });
    function create(): ComponentFixture<AdminAiPromptWorkbenchComponent> {
        const fixture = TestBed.createComponent(AdminAiPromptWorkbenchComponent);
        fixture.componentRef.setInput('scenarioKey', 'text-parse');
        fixture.componentRef.setInput('locale', 'en');
        fixture.componentRef.setInput('promptText', 'Parse {{userText}}');
        fixture.detectChanges();
        fixture.componentInstance['text'].set('an apple');
        fixture.detectChanges();
        return fixture;
    }

    it('requires an image for a product-label test and passes the separate scenario', async () => {
        const fixture = create();
        const component = fixture.componentInstance;
        fixture.componentRef.setInput('scenarioKey', 'product-label');
        fixture.componentRef.setInput('promptText', 'Read the label');
        fixture.detectChanges();
        await component['inspectAsync'](false);
        expect(component['requiresImage']()).toBe(true);
        expect(component['canTest']()).toBe(false);
        await component['inspectAsync'](true);
        expect(api.test).not.toHaveBeenCalled();
        component['imageAssetId'].set('label-image');
        fixture.detectChanges();
        await component['inspectAsync'](false);
        expect(component['canTest']()).toBe(true);
        await component['inspectAsync'](true);
        expect(api.test).toHaveBeenCalledWith(expect.objectContaining({ key: 'product-label', imageAssetId: 'label-image' }));
    });

    it('ignores preview responses for a draft edited while the request was in flight', async () => {
        const pending = new Subject<{ text: string }>();
        api.preview.mockReturnValue(pending);
        const fixture = create();
        const verified = vi.fn();
        fixture.componentInstance.verified.subscribe(verified);
        const request = fixture.componentInstance['inspectAsync'](false);
        fixture.componentRef.setInput('promptText', 'New {{userText}}');
        fixture.detectChanges();
        pending.next({ text: 'Old preview' });
        pending.complete();
        await request;
        expect(fixture.componentInstance['preview']()).toBe('');
        expect(verified).not.toHaveBeenCalled();
        expect(fixture.componentInstance['canTest']()).toBe(false);
    });

    it('requires a fresh preview after the sample changes and runs tests only on explicit action', async () => {
        const fixture = create();
        const component = fixture.componentInstance;
        await component['inspectAsync'](true);
        expect(api.test).not.toHaveBeenCalled();
        await component['inspectAsync'](false);
        expect(component['canTest']()).toBe(true);
        await component['inspectAsync'](true);
        expect(api.test).toHaveBeenCalledWith(
            expect.objectContaining({ text: 'an apple', promptText: 'Parse {{userText}}', locale: 'en' }),
        );
        component['text'].set('two eggs');
        fixture.detectChanges();
        expect(component['canTest']()).toBe(false);
        expect(component['result']()).toBe('');
    });
});

describe('AdminAiPromptWorkbenchComponent AI consent error', () => {
    it.each([
        { status: HttpStatusCode.Forbidden, code: 'Ai.ConsentRequired', key: 'ADMIN_PROMPTS.CONSENT_REQUIRED' },
        { status: HttpStatusCode.Forbidden, code: 'Access.Forbidden', key: 'ADMIN_PROMPTS.TEST_ERROR' },
        { status: HttpStatusCode.TooManyRequests, code: 'Ai.QuotaExceeded', key: 'ADMIN_PROMPTS.TEST_LIMIT' },
    ])('explains $code and preserves the sample and checked preview', async ({ status, code, key }) => {
        await TestBed.configureTestingModule({
            imports: [AdminAiPromptWorkbenchComponent],
            providers: [
                ...provideTranslateTesting(),
                {
                    provide: AdminAiPromptsFacade,
                    useValue: {
                        preview: (): Observable<{ text: string }> => of({ text: 'Checked request' }),
                        test: (): Observable<never> => throwError(() => new HttpErrorResponse({ status, error: { error: code } })),
                    },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminAiPromptWorkbenchComponent);
        fixture.componentRef.setInput('scenarioKey', 'text-parse');
        fixture.componentRef.setInput('locale', 'en');
        fixture.componentRef.setInput('promptText', 'Parse {{userText}}');
        fixture.detectChanges();
        const component = fixture.componentInstance;
        component['text'].set('an apple');
        fixture.detectChanges();
        await component['inspectAsync'](false);
        await component['inspectAsync'](true);
        fixture.detectChanges();
        expect(component['errorKey']()).toBe(key);
        expect(component['text']()).toBe('an apple');
        expect(component['preview']()).toBe('Checked request');
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(key);
    });
});
