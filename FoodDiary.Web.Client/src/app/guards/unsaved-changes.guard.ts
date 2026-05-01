import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { from, isObservable, Observable, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import {
    UnsavedChangesDialogComponent,
    UnsavedChangesDialogResult,
} from '../components/shared/unsaved-changes-dialog/unsaved-changes-dialog.component';
import { UnsavedChangesService } from '../services/unsaved-changes.service';

const toObservable = (value: unknown): Observable<unknown> => {
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
            preset: 'confirm',
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
