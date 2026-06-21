import { inject } from '@angular/core';
import type { CanDeactivateFn } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { from, isObservable, type Observable, of, switchMap } from 'rxjs';

import {
    UnsavedChangesDialogComponent,
    type UnsavedChangesDialogResult,
} from '../components/shared/unsaved-changes-dialog/unsaved-changes-dialog';
import { UnsavedChangesService } from '../services/unsaved-changes.service';

const isPromiseLike = (value: unknown): value is PromiseLike<unknown> =>
    typeof value === 'object' && value !== null && 'then' in value && typeof value.then === 'function';

const toObservable = (value: unknown): Observable<unknown> => {
    if (isObservable(value)) {
        return value;
    }
    if (isPromiseLike(value)) {
        return from(value);
    }
    return of(value);
};

export const unsavedChangesGuard: CanDeactivateFn<unknown> = () => {
    const unsavedChangesService = inject(UnsavedChangesService);
    const dialogService = inject(FdUiDialogService);
    const handler = unsavedChangesService.getHandler();

    if (handler?.hasChanges() !== true) {
        return true;
    }

    return dialogService
        .open<UnsavedChangesDialogComponent, null, UnsavedChangesDialogResult>(UnsavedChangesDialogComponent, {
            preset: 'confirm',
        })
        .afterClosed()
        .pipe(
            switchMap(result => {
                if (result === 'save') {
                    return toObservable(handler.save()).pipe(switchMap(saveResult => of(saveResult !== false)));
                }
                if (result === 'discard') {
                    handler.discard();
                    return of(true);
                }
                return of(false);
            }),
        );
};
