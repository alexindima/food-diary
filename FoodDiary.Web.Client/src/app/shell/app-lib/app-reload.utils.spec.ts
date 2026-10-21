import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ChildrenOutletContexts, PRIMARY_OUTLET, provideRouter, RouterOutlet } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { UnsavedEditor } from '../../shared/forms/unsaved-editor';
import { confirmRoutedEditorsReloadAsync } from './app-reload.utils';

@Component({
    selector: 'fd-test-reload-container',
    imports: [RouterOutlet],
    template: '<router-outlet />',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ReloadContainerComponent {}

@Component({ selector: 'fd-test-reload-editor', template: 'Draft', changeDetection: ChangeDetectionStrategy.OnPush })
class ReloadEditorComponent implements UnsavedEditor {
    public readonly canLeaveAsync = vi.fn<() => Promise<boolean>>().mockResolvedValue(false);
}

describe('confirmRoutedEditorsReloadAsync', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                provideRouter([
                    {
                        path: 'container',
                        component: ReloadContainerComponent,
                        children: [{ path: 'edit', component: ReloadEditorComponent }],
                    },
                    { path: 'plain', component: ReloadContainerComponent },
                ]),
            ],
        });
    });

    it.each([false, true])('honors the nested editor decision: %s', async allowed => {
        await RouterTestingHarness.create('/container/edit');
        const contexts = TestBed.inject(ChildrenOutletContexts);
        const page: unknown = contexts.getContext(PRIMARY_OUTLET)?.children.getContext(PRIMARY_OUTLET)?.outlet?.component;
        if (!(page instanceof ReloadEditorComponent)) {
            throw new Error('Nested editor did not activate');
        }
        page.canLeaveAsync.mockResolvedValue(allowed);

        expect(await confirmRoutedEditorsReloadAsync(contexts)).toBe(allowed);
        expect(page.canLeaveAsync).toHaveBeenCalledOnce();
    });

    it('allows reloading ordinary pages without an editor contract', async () => {
        await RouterTestingHarness.create('/plain');
        expect(await confirmRoutedEditorsReloadAsync(TestBed.inject(ChildrenOutletContexts))).toBe(true);
    });
});
