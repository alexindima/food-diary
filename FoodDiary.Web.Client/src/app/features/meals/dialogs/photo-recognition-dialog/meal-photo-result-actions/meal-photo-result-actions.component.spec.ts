import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { describe, expect, it, vi } from 'vitest';

import { MealPhotoResultActionsComponent } from './meal-photo-result-actions.component';

describe('MealPhotoResultActionsComponent', () => {
    it('should expose edit action state for view mode', async () => {
        const { component, fixture } = await setupComponentAsync({ isEditing: false });

        fixture.detectChanges();

        expect(component.editActionState()).toEqual({
            variant: 'secondary',
            fill: 'outline',
            labelKey: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON',
        });
    });

    it('should expose edit action state for edit mode', async () => {
        const { component, fixture } = await setupComponentAsync({ isEditing: true });

        fixture.detectChanges();

        expect(component.editActionState()).toEqual({
            variant: 'primary',
            fill: 'solid',
            labelKey: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.SAVE',
        });
    });

    it('should disable reanalyze action while editing or without selected asset', async () => {
        const { fixture } = await setupComponentAsync({ hasSelectionAsset: false, isEditing: false });

        fixture.detectChanges();
        const buttons = fixture.debugElement.queryAll(By.directive(FdUiButtonComponent));

        expect((buttons[0].componentInstance as FdUiButtonComponent).disabled()).toBe(true);
    });

    it('should emit result actions from buttons', async () => {
        const { component, fixture } = await setupComponentAsync({ hasSelectionAsset: true });
        const reanalyzeSpy = vi.fn();
        const editActionSpy = vi.fn();
        component.reanalyze.subscribe(reanalyzeSpy);
        component.editAction.subscribe(editActionSpy);

        fixture.detectChanges();
        const buttons = fixture.debugElement.queryAll(By.directive(FdUiButtonComponent));
        buttons[0].triggerEventHandler('click');
        buttons[1].triggerEventHandler('click');

        expect(reanalyzeSpy).toHaveBeenCalledOnce();
        expect(editActionSpy).toHaveBeenCalledOnce();
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        hasSelectionAsset: boolean;
        isEditing: boolean;
    }> = {},
): Promise<{
    component: MealPhotoResultActionsComponent;
    fixture: ComponentFixture<MealPhotoResultActionsComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealPhotoResultActionsComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealPhotoResultActionsComponent);
    fixture.componentRef.setInput('hasSelectionAsset', overrides.hasSelectionAsset ?? true);
    fixture.componentRef.setInput('isEditing', overrides.isEditing ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
