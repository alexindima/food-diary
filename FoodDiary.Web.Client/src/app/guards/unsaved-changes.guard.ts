import { CanDeactivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { from, isObservable, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { UnsavedChangesService } from '../services/unsaved-changes.service';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { UnsavedChangesDialogComponent, UnsavedChangesDialogResult } from '../components/shared/unsaved-changes-dialog/unsaved-changes-dialog.component';

const toObservable = (value: unknown) => {
    if (isObservable(value)) {
        return value;
    }
    return from(Promise.resolve(value));
};

export const unsavedChangesGuard: CanDeactivateFn<unknown> = () => {
    const unsavedChangesService = inject(UnsavedChangesService);
    const dialogService = inject(FdUiDialogService);
    const handler = unsavedChangesService.getHandler();

    if (!handler || !handler.hasChanges()) {
        return true;
    }

    return dialogService
        .open<UnsavedChangesDialogComponent, null, UnsavedChangesDialogResult>(UnsavedChangesDialogComponent, {
            size: 'sm',
        })
        .afterClosed()
        .pipe(
            switchMap(result => {
                if (result === 'save') {
                    return toObservable(handler.save()).pipe(switchMap(() => of(true)));
                }
                if (result === 'discard') {
                    handler.discard();
                    return of(true);
                }
                return of(false);
            }),
        );
};
