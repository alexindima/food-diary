import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, email, form, required } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Subscription } from 'rxjs';
import { finalize } from 'rxjs';

import { RequestStateController } from '../../../shared/lib/request-state';
import type { DietologistPermissions, DietologistRelationship } from '../../../shared/models/dietologist.data';
import { DIETOLOGIST_RELATIONSHIP_ACTIONS } from '../../dietologist/contracts/relationship-actions';
import { ProfileManageFacade } from './profile-manage.facade';
import { DEFAULT_DIETOLOGIST_PERMISSIONS } from './user-manage.config';
import type { DietologistFormValues, DietologistPermissionChange, DietologistPermissionControlName } from './user-manage.types';
import { getDietologistPermissions, mapDietologistRelationshipToForm } from './user-manage-dietologist-form.mapper';
import { createDietologistFormModel } from './user-manage-form.mapper';

@Injectable()
export class ProfileDietologistFacade {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly toastService = inject(FdUiToastService);
    private readonly facade = inject(ProfileManageFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly dietologistFacade = inject(DIETOLOGIST_RELATIONSHIP_ACTIONS);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    public readonly dietologistFormModel = signal<DietologistFormValues>(createDietologistFormModel());

    public readonly dietologistForm = form(this.dietologistFormModel, path => {
        required(path.email);
        email(path.email);
        disabled(path.email, { when: () => this.hasDietologistRelationship() });
    });

    public readonly dietologistRelationship = this.facade.dietologistRelationship;

    public readonly dietologistError = signal<string | null>(null);

    public readonly dietologistPermissions = signal<DietologistPermissions>(DEFAULT_DIETOLOGIST_PERMISSIONS);

    private readonly relationshipRequest = new RequestStateController<true>();
    private relationshipRead: Subscription | undefined;
    public readonly isLoadingDietologist = this.relationshipRequest.isLoading;

    public readonly isSavingDietologistPermissions = signal(false);

    public readonly isSavingDietologistRelationshipAction = signal(false);

    public readonly isSavingDietologist = computed(
        () => this.isSavingDietologistPermissions() || this.isSavingDietologistRelationshipAction(),
    );

    public readonly hasDietologistRelationship = computed(() => this.dietologistRelationship() !== null);

    public readonly isDietologistPending = computed(() => this.dietologistRelationship()?.status === 'Pending');

    public readonly isDietologistConnected = computed(() => this.dietologistRelationship()?.status === 'Accepted');

    public readonly dietologistInviteEmailError = signal<string | null>(null);

    public watchDietologistRelationship(): void {
        effect(() => {
            this.syncDietologistFormFromRelationship(this.facade.dietologistRelationship());
        });
    }

    public watchDietologistFormChanges(): void {
        effect(() => {
            this.dietologistFormModel();
            this.updateDietologistPermissionsState();
            this.updateDietologistInviteEmailError();
        });
    }

    public inviteDietologist(): void {
        if (this.isSavingDietologistRelationshipAction()) {
            return;
        }

        this.dietologistForm.email().markAsTouched();
        this.updateDietologistInviteEmailError();
        if (this.dietologistForm().invalid()) {
            return;
        }

        this.cancelRelationshipRead();
        this.isSavingDietologistRelationshipAction.set(true);
        this.dietologistFacade
            .invite({
                dietologistEmail: this.dietologistFormModel().email,
                permissions: getDietologistPermissions(this.dietologistFormModel()),
            })
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isSavingDietologistRelationshipAction.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.toastService.success(this.translateService.instant('USER_MANAGE.DIETOLOGIST_INVITE_SUCCESS'));
                    this.loadDietologistRelationship();
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_INVITE_ERROR');
                },
            });
    }

    public updateDietologistPermission(controlName: DietologistPermissionControlName, nextValue: boolean): void {
        if (this.isSavingDietologist()) {
            return;
        }

        const previousPermissions = getDietologistPermissions(this.dietologistFormModel());
        this.dietologistForm[controlName]().value.set(nextValue);
        this.updateDietologistPermissionsState();
        if (this.hasDietologistRelationship()) {
            this.persistDietologistPermissions(previousPermissions);
        }
    }

    public onDietologistPermissionChangeRequest(change: DietologistPermissionChange): void {
        this.updateDietologistPermission(change.controlName, change.value);
    }

    public persistDietologistPermissions(previousPermissions?: DietologistPermissions): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologistPermissions()) {
            return;
        }

        const nextPermissions = getDietologistPermissions(this.dietologistFormModel());
        this.dietologistError.set(null);
        this.cancelRelationshipRead();
        this.isSavingDietologistPermissions.set(true);
        this.dietologistFacade
            .updatePermissions(nextPermissions)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isSavingDietologistPermissions.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.dietologistError.set(null);
                    this.updateDietologistRelationshipPermissions(nextPermissions);
                },
                error: () => {
                    if (previousPermissions !== undefined) {
                        this.dietologistForm().reset({
                            ...this.dietologistFormModel(),
                            ...previousPermissions,
                        });
                        this.updateDietologistPermissionsState();
                    }

                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');
                },
            });
    }

    public revokeDietologistRelationship(): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologistRelationshipAction()) {
            return;
        }

        if (this.isDietologistConnected()) {
            this.dialogService
                .open(FdUiConfirmDialogComponent, {
                    preset: 'confirm',
                    data: {
                        title: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_TITLE'),
                        message: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_MESSAGE'),
                        confirmLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_CONFIRM'),
                        cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                    },
                })
                .afterClosed()
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(confirmed => {
                    if (confirmed === true) {
                        this.executeDietologistRevoke();
                    }
                });
            return;
        }

        this.executeDietologistRevoke();
    }

    public onDietologistProfileToggle(nextValue: boolean): void {
        if (this.isSavingDietologistPermissions()) {
            return;
        }

        if (nextValue) {
            this.dietologistForm.shareProfile().value.set(nextValue);
            if (this.hasDietologistRelationship()) {
                this.persistDietologistPermissions({
                    ...getDietologistPermissions(this.dietologistFormModel()),
                    shareProfile: !nextValue,
                });
            }
            return;
        }

        this.dialogService
            .open(FdUiConfirmDialogComponent, {
                preset: 'confirm',
                data: {
                    title: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_TITLE'),
                    message: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_MESSAGE'),
                    confirmLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CONFIRM'),
                    cancelLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CANCEL'),
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirmed => {
                if (confirmed === true) {
                    const previousPermissions = getDietologistPermissions(this.dietologistFormModel());
                    this.dietologistForm.shareProfile().value.set(false);
                    if (this.hasDietologistRelationship()) {
                        this.persistDietologistPermissions(previousPermissions);
                    }
                }
            });
    }

    public updateDietologistInviteEmailError(): void {
        this.dietologistInviteEmailError.set(
            resolveSignalFormFieldError(this.dietologistForm.email, this.validationErrors, this.translateService),
        );
    }

    public loadDietologistRelationship(): void {
        this.relationshipRead?.unsubscribe();
        const requestId = this.relationshipRequest.begin();
        this.relationshipRead = this.dietologistFacade
            .getRelationship()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: relationship => {
                    if (this.relationshipRequest.succeed(requestId, true)) {
                        this.facade.dietologistRelationship.set(relationship);
                        this.dietologistError.set(null);
                    }
                },
                error: () => {
                    if (this.relationshipRequest.fail(requestId, 'USER_MANAGE.DIETOLOGIST_LOAD_ERROR')) {
                        this.setDietologistError('USER_MANAGE.DIETOLOGIST_LOAD_ERROR');
                    }
                },
            });
    }
    private cancelRelationshipRead(): void {
        this.relationshipRead?.unsubscribe();
        this.relationshipRequest.reset();
    }

    public syncDietologistFormFromRelationship(relationship: DietologistRelationship | null): void {
        const model = mapDietologistRelationshipToForm(relationship);
        this.dietologistForm().reset(model);
        this.dietologistPermissions.set(getDietologistPermissions(model));
    }

    public updateDietologistPermissionsState(): void {
        this.dietologistPermissions.set(getDietologistPermissions(this.dietologistFormModel()));
    }

    public updateDietologistRelationshipPermissions(permissions: DietologistPermissions): void {
        const relationship = this.facade.dietologistRelationship();
        if (relationship === null) {
            return;
        }

        this.facade.dietologistRelationship.set({
            ...relationship,
            permissions,
        });
    }

    public setDietologistError(errorKey: string): void {
        this.dietologistError.set(this.translateService.instant(errorKey));
    }

    public executeDietologistRevoke(): void {
        this.cancelRelationshipRead();
        this.isSavingDietologistRelationshipAction.set(true);
        this.dietologistFacade
            .revokeRelationship()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isSavingDietologistRelationshipAction.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.toastService.info(this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECTED'));
                    this.facade.dietologistRelationship.set(null);
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_DISCONNECT_ERROR');
                },
            });
    }
    public constructor() {
        this.watchDietologistRelationship();
        this.watchDietologistFormChanges();
        this.updateDietologistInviteEmailError();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updateDietologistInviteEmailError();
        });
    }
}
