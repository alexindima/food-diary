import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiDialogComponent } from './fd-ui-dialog';
import { type FdUiDialogConfig, FdUiDialogService } from './fd-ui-dialog.service';
import { FdUiDialogHeaderDirective } from './fd-ui-dialog-header.directive';

@Component({
    imports: [FdUiDialogComponent, FdUiDialogHeaderDirective],
    template: `
        <fd-ui-dialog [title]="title()">
            @if (customHeader()) {
                <div fdUiDialogHeader>
                    @if (customHeading()) {
                        <h2>{{ customHeading() }}</h2>
                    } @else {
                        Custom visible header
                    }
                </div>
            }
            <button type="button">Action</button>
        </fd-ui-dialog>
    `,
})
class DialogAccessibleNameHostComponent {
    public readonly title = signal<string | undefined>('Shopping merge');
    public readonly customHeader = signal(false);
    public readonly customHeading = signal<string | null>(null);
}

beforeEach(() => {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false, addListener: vi.fn(), removeListener: vi.fn() })),
    );
});
afterEach(() => {
    vi.unstubAllGlobals();
});

async function openAccessibleDialogAsync(config: FdUiDialogConfig = {}): Promise<{
    host: DialogAccessibleNameHostComponent;
    container: HTMLElement;
    renderAsync: () => Promise<void>;
}> {
    TestBed.configureTestingModule({
        imports: [DialogAccessibleNameHostComponent],
        providers: [provideTranslateTesting()],
    });
    const fixture = TestBed.createComponent(DialogAccessibleNameHostComponent);
    fixture.detectChanges();
    const ref = TestBed.inject(FdUiDialogService).open(DialogAccessibleNameHostComponent, config);
    const renderAsync = async (): Promise<void> => {
        TestBed.tick();
        await fixture.whenStable();
    };
    await renderAsync();
    const container = document.getElementById(ref.id);
    const host = ref.componentInstance;
    if (container === null || host === null) {
        throw new Error('Expected the opened dialog and its container.');
    }
    return { host, container, renderAsync };
}

describe('Overlay dialog accessible naming', () => {
    it('exposes one dialog named by the visible built-in heading', async () => {
        const { container } = await openAccessibleDialogAsync();
        expect(container.querySelector('[role="dialog"]')).toBeNull();
        const headingId = container.getAttribute('aria-labelledby');
        expect(headingId).toBeTruthy();
        expect(document.getElementById(headingId ?? '')?.textContent).toBe('Shopping merge');
    });

    it('keeps the accessible name current when the title changes', async () => {
        const { container, host, renderAsync } = await openAccessibleDialogAsync();
        host.title.set('Updated merge preview');
        await renderAsync();
        expect(document.getElementById(container.getAttribute('aria-labelledby') ?? '')?.textContent).toBe('Updated merge preview');
    });

    it.each([{ ariaLabel: 'Caller label' }, { ariaLabelledBy: 'caller-heading' }])(
        'preserves an explicit caller name: %j',
        async config => {
            const { container, host, renderAsync } = await openAccessibleDialogAsync(config);
            host.title.set('Updated title');
            await renderAsync();
            if (config.ariaLabel !== undefined) {
                expect(container.getAttribute('aria-label')).toBe(config.ariaLabel);
            } else {
                expect(container.getAttribute('aria-labelledby')).toBe(config.ariaLabelledBy);
            }
        },
    );

    it('uses the supplied title when a custom header replaces the built-in heading', async () => {
        const { container, host, renderAsync } = await openAccessibleDialogAsync();
        host.customHeader.set(true);
        await renderAsync();
        expect(container.querySelector('.fd-ui-dialog__title')).toBeNull();
        expect(container.getAttribute('aria-label')).toBe('Shopping merge');
        expect(container.getAttribute('aria-labelledby')).toBeNull();
        expect(container.querySelector('[role="dialog"]')).toBeNull();
    });

    it('names projected detail headers by their heading when no title input is supplied', async () => {
        const { container, host, renderAsync } = await openAccessibleDialogAsync();
        host.title.set(undefined);
        host.customHeader.set(true);
        host.customHeading.set('Recipe detail');
        await renderAsync();
        const headingId = container.getAttribute('aria-labelledby');
        expect(headingId).toBeTruthy();
        expect(document.getElementById(headingId ?? '')?.textContent).toBe('Recipe detail');
        host.customHeading.set('Updated recipe');
        await renderAsync();
        expect(document.getElementById(headingId ?? '')?.textContent).toBe('Updated recipe');
    });
});

describe('Standalone dialog accessible naming', () => {
    it('retains modal semantics and names a custom header without a missing heading reference', async () => {
        TestBed.configureTestingModule({
            imports: [DialogAccessibleNameHostComponent],
            providers: [provideTranslateTesting()],
        });
        const fixture = TestBed.createComponent(DialogAccessibleNameHostComponent);
        fixture.componentInstance.customHeader.set(true);
        fixture.detectChanges();
        await fixture.whenStable();
        const dialog = (fixture.nativeElement as HTMLElement).querySelector('[role="dialog"]');
        expect(dialog?.getAttribute('aria-modal')).toBe('true');
        expect(dialog?.getAttribute('aria-label')).toBe('Shopping merge');
        expect(dialog?.getAttribute('aria-labelledby')).toBeNull();
    });
});
