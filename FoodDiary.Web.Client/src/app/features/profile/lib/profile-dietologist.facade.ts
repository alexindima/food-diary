import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { Injectable } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, email, form, required } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize, tap } from 'rxjs';

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

    private readonly relationshipLoading = signal(false);
    private readonly relationshipRequest = signal<symbol | undefined>(undefined);
    private readonly relationshipResource = rxResource({
        params: this.relationshipRequest,
        stream: ({ params }) =>
            this.dietologistFacade.getRelationship().pipe(
                tap({
                    next: relationship => {
                        if (this.relationshipRequest() === params) {
                            this.relationshipLoading.set(false);
                            this.facade.setDietologistRelationship(relationship);
                            this.dietologistError.set(null);
                        }
                    },
                    error: () => {
                        if (this.relationshipRequest() === params) {
                            this.relationshipLoading.set(false);
                            this.setDietologistError('USER_MANAGE.DIETOLOGIST_LOAD_ERROR');
                        }
                    },
                }),
            ),
    });
    public readonly isLoadingDietologist = this.relationshipLoading.asReadonly();

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

        this.dietologistForm[controlName]().value.set(nextValue);
        this.updateDietologistPermissionsState();
        const relationship = this.dietologistRelationship();
        if (relationship !== null) {
            this.persistDietologistPermissions(relationship.invitationId);
        }
    }

    public onDietologistPermissionChangeRequest(change: DietologistPermissionChange): void {
        this.updateDietologistPermission(change.controlName, change.value);
    }

    private persistDietologistPermissions(invitationId: DietologistRelationship['invitationId']): void {
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
                    const relationship = this.dietologistRelationship();
                    if (relationship?.invitationId !== invitationId) {
                        return;
                    }

                    this.dietologistError.set(null);
                    this.facade.setDietologistRelationship({ ...relationship, permissions: nextPermissions });
                    this.refreshPendingRelationshipRead();
                },
                error: () => {
                    const relationship = this.dietologistRelationship();
                    if (relationship?.invitationId !== invitationId) {
                        return;
                    }

                    this.syncDietologistFormFromRelationship(relationship);
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');
                },
            });
    }

    public revokeDietologistRelationship(): void {
        const invitationId = this.dietologistRelationship()?.invitationId;
        if (invitationId === undefined || this.isSavingDietologistRelationshipAction()) {
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
                    if (confirmed === true && this.dietologistRelationship()?.invitationId === invitationId) {
                        this.executeDietologistRevoke(invitationId);
                    }
                });
            return;
        }

        this.executeDietologistRevoke(invitationId);
    }

    public onDietologistProfileToggle(nextValue: boolean): void {
        if (this.isSavingDietologistPermissions()) {
            return;
        }

        const invitationId = this.dietologistRelationship()?.invitationId;
        if (nextValue) {
            this.dietologistForm.shareProfile().value.set(nextValue);
            if (invitationId !== undefined) {
                this.persistDietologistPermissions(invitationId);
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
                if (confirmed === true && this.dietologistRelationship()?.invitationId === invitationId) {
                    this.dietologistForm.shareProfile().value.set(false);
                    if (invitationId !== undefined && !this.isSavingDietologistPermissions()) {
                        this.persistDietologistPermissions(invitationId);
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
        this.relationshipLoading.set(true);
        this.relationshipRequest.set(Symbol());
    }
    private cancelRelationshipRead(): void {
        this.relationshipRequest.set(undefined);
        this.relationshipResource.set(undefined);
        this.relationshipLoading.set(false);
    }

    private refreshPendingRelationshipRead(): void {
        // A refresh started before the write was acknowledged may return an older relationship.
        if (this.isLoadingDietologist()) {
            this.loadDietologistRelationship();
        }
    }

    public syncDietologistFormFromRelationship(relationship: DietologistRelationship | null): void {
        const model = mapDietologistRelationshipToForm(relationship);
        this.dietologistForm().reset(model);
        this.dietologistPermissions.set(getDietologistPermissions(model));
    }

    public updateDietologistPermissionsState(): void {
        this.dietologistPermissions.set(getDietologistPermissions(this.dietologistFormModel()));
    }

    public setDietologistError(errorKey: string): void {
        this.dietologistError.set(this.translateService.instant(errorKey));
    }

    private executeDietologistRevoke(invitationId: DietologistRelationship['invitationId']): void {
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
                    if (this.dietologistRelationship()?.invitationId !== invitationId) {
                        return;
                    }

                    this.toastService.info(this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECTED'));
                    this.facade.setDietologistRelationship(null);
                    this.dietologistError.set(null);
                    this.refreshPendingRelationshipRead();
                },
                error: () => {
                    if (this.dietologistRelationship()?.invitationId === invitationId) {
                        this.setDietologistError('USER_MANAGE.DIETOLOGIST_DISCONNECT_ERROR');
                    }
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
