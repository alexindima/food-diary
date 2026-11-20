import { OverlayContainer } from '@angular/cdk/overlay';
import { Component, Directive, inject, InjectionToken } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../src/testing/async-testing';
import { FdUiMenuComponent } from './fd-ui-menu';
import { FdUiMenuItemComponent } from './fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from './fd-ui-menu-trigger.directive';

type RecipeCommands = { rename: () => void; archive: () => void };
const RECIPE_COMMANDS = new InjectionToken<RecipeCommands>('Recipe commands');

@Directive({ selector: '[recipeCommandScope]', exportAs: 'recipeCommandScope' })
class RecipeCommandScopeDirective {
    public readonly commands = inject(RECIPE_COMMANDS);
}

@Component({
    imports: [FdUiMenuComponent, FdUiMenuItemComponent, FdUiMenuTriggerDirective, RecipeCommandScopeDirective],
    template: `
        <button type="button" [fdUiMenuTrigger]="menu">Recipe actions</button>
        <fd-ui-menu #menu="fdUiMenu">
            <fd-ui-menu-item recipeCommandScope #rename="recipeCommandScope" (itemClick)="rename.commands.rename()">
                Rename recipe
            </fd-ui-menu-item>
            <div role="group" aria-label="Publication">
                <fd-ui-menu-item [disabled]="true">Publish recipe</fd-ui-menu-item>
                <fd-ui-menu-item recipeCommandScope #archive="recipeCommandScope" (itemClick)="archive.commands.archive()">
                    Archive recipe
                </fd-ui-menu-item>
            </div>
        </fd-ui-menu>
    `,
})
class RecipeMenuHostComponent {}

it('preserves scoped commands, projected item navigation and trigger focus in a locally configured menu', async () => {
    const parentCommands = { rename: vi.fn(), archive: vi.fn() };
    const recipeCommands = { rename: vi.fn(), archive: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [RecipeMenuHostComponent],
        providers: [provideRouter([]), { provide: RECIPE_COMMANDS, useValue: parentCommands }],
    })
        .overrideComponent(FdUiMenuComponent, { add: { providers: [{ provide: RECIPE_COMMANDS, useValue: recipeCommands }] } })
        .compileComponents();

    const fixture = TestBed.createComponent(RecipeMenuHostComponent);
    fixture.detectChanges();
    const trigger = requireButton(fixture.nativeElement as HTMLElement, 'Recipe actions');
    const overlayRoot = TestBed.inject(OverlayContainer).getContainerElement();
    trigger.focus();
    dispatchKey(trigger, 'ArrowDown');
    await settleOverlayAsync(fixture);
    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    expect(overlayRoot.querySelector('[role="group"]')?.getAttribute('aria-label')).toBe('Publication');
    const rename = requireButton(overlayRoot, 'Rename recipe');
    const archive = requireButton(overlayRoot, 'Archive recipe');
    expect(document.activeElement).toBe(rename);
    expect(requireButton(overlayRoot, 'Publish recipe').disabled).toBe(true);

    dispatchKey(rename, 'ArrowDown');
    expect(document.activeElement).toBe(archive);
    dispatchKey(archive, 'Escape');
    await settleOverlayAsync(fixture);
    expect(overlayRoot.querySelector('[role="menu"]')).toBeNull();
    expect(document.activeElement).toBe(trigger);
    expect(recipeCommands.archive).not.toHaveBeenCalled();

    dispatchKey(trigger, 'ArrowUp');
    await settleOverlayAsync(fixture);
    const reopenedArchive = requireButton(overlayRoot, 'Archive recipe');
    expect(document.activeElement).toBe(reopenedArchive);
    reopenedArchive.click();
    await settleOverlayAsync(fixture);
    expect(recipeCommands.archive).toHaveBeenCalledOnce();
    expect(recipeCommands.rename).not.toHaveBeenCalled();
    expect(parentCommands.archive).not.toHaveBeenCalled();
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    expect(document.activeElement).toBe(trigger);
});

async function settleOverlayAsync(fixture: ComponentFixture<RecipeMenuHostComponent>): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    await waitForAsyncTasksAsync();
    fixture.detectChanges();
}

function requireButton(root: HTMLElement, label: string): HTMLButtonElement {
    const button = [...root.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected the ${label} button.`);
    }
    return button;
}

function dispatchKey(target: HTMLElement, key: string): void {
    target.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
}
