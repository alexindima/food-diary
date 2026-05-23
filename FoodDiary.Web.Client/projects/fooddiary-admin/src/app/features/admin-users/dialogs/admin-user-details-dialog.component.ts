import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { forkJoin } from 'rxjs';

import { type AdminUser, type AdminUserLoginEvent, type AdminUserRoleAuditEvent, AdminUsersService } from '../api/admin-users.service';

type DetailField = {
    label: string;
    value: string;
};

type DetailSection = {
    title: string;
    fields: DetailField[];
};

export type AdminUserDetailsDialogResult = 'edit' | 'impersonate' | null;

@Component({
    selector: 'fd-admin-user-details-dialog',
    imports: [DatePipe, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
    templateUrl: './admin-user-details-dialog.component.html',
    styleUrl: './admin-user-details-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserDetailsDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<AdminUserDetailsDialogComponent, AdminUserDetailsDialogResult>>(FdUiDialogRef);
    private readonly initialUser = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly usersService = inject(AdminUsersService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly user = signal<AdminUser>(this.initialUser);
    public readonly loginEvents = signal<AdminUserLoginEvent[]>([]);
    public readonly roleAuditEvents = signal<AdminUserRoleAuditEvent[]>([]);
    public readonly isLoading = signal(true);
    public readonly hasError = signal(false);
    public readonly initials = computed(() => this.buildInitials(this.user()));
    public readonly canImpersonate = computed(() => {
        const currentUser = this.user();
        return currentUser.deletedAt === null || currentUser.deletedAt === undefined ? !currentUser.roles.includes('Admin') : false;
    });

    public readonly sections = computed<DetailSection[]>(() => {
        const currentUser = this.user();
        return [
            {
                title: 'Account',
                fields: [
                    { label: 'User ID', value: currentUser.id },
                    { label: 'Email', value: currentUser.email },
                    { label: 'Username', value: this.text(currentUser.username) },
                    { label: 'Roles', value: currentUser.roles.join(', ') || '-' },
                    { label: 'Active', value: this.boolean(currentUser.isActive) },
                    { label: 'Email confirmed', value: this.boolean(currentUser.isEmailConfirmed) },
                    { label: 'Has password', value: this.boolean(currentUser.hasPassword) },
                    { label: 'Telegram user ID', value: this.number(currentUser.telegramUserId) },
                ],
            },
            {
                title: 'Profile',
                fields: [
                    { label: 'First name', value: this.text(currentUser.firstName) },
                    { label: 'Last name', value: this.text(currentUser.lastName) },
                    { label: 'Birth date', value: this.date(currentUser.birthDate) },
                    { label: 'Gender', value: this.text(currentUser.gender) },
                    { label: 'Height', value: this.number(currentUser.height) },
                    { label: 'Weight', value: this.number(currentUser.weight) },
                    { label: 'Desired weight', value: this.number(currentUser.desiredWeight) },
                    { label: 'Desired waist', value: this.number(currentUser.desiredWaist) },
                    { label: 'Activity level', value: this.text(currentUser.activityLevel) },
                ],
            },
            {
                title: 'Goals',
                fields: [
                    { label: 'Daily calories', value: this.number(currentUser.dailyCalorieTarget) },
                    { label: 'Protein target', value: this.number(currentUser.proteinTarget) },
                    { label: 'Fat target', value: this.number(currentUser.fatTarget) },
                    { label: 'Carb target', value: this.number(currentUser.carbTarget) },
                    { label: 'Fiber target', value: this.number(currentUser.fiberTarget) },
                    { label: 'Water goal', value: this.number(currentUser.waterGoal) },
                    { label: 'Hydration goal', value: this.number(currentUser.hydrationGoal) },
                    { label: 'Step goal', value: this.number(currentUser.stepGoal) },
                    { label: 'Calorie cycling', value: this.boolean(currentUser.calorieCyclingEnabled) },
                ],
            },
            {
                title: 'Calorie cycling',
                fields: [
                    { label: 'Monday', value: this.number(currentUser.mondayCalories) },
                    { label: 'Tuesday', value: this.number(currentUser.tuesdayCalories) },
                    { label: 'Wednesday', value: this.number(currentUser.wednesdayCalories) },
                    { label: 'Thursday', value: this.number(currentUser.thursdayCalories) },
                    { label: 'Friday', value: this.number(currentUser.fridayCalories) },
                    { label: 'Saturday', value: this.number(currentUser.saturdayCalories) },
                    { label: 'Sunday', value: this.number(currentUser.sundayCalories) },
                ],
            },
            {
                title: 'Settings',
                fields: [
                    { label: 'Language', value: this.text(currentUser.language) },
                    { label: 'Theme', value: this.text(currentUser.theme) },
                    { label: 'UI style', value: this.text(currentUser.uiStyle) },
                    { label: 'Push notifications', value: this.boolean(currentUser.pushNotificationsEnabled) },
                    { label: 'Fasting notifications', value: this.boolean(currentUser.fastingPushNotificationsEnabled) },
                    { label: 'Social notifications', value: this.boolean(currentUser.socialPushNotificationsEnabled) },
                    { label: 'Fasting reminder hours', value: this.number(currentUser.fastingCheckInReminderHours) },
                    { label: 'Fasting follow-up hours', value: this.number(currentUser.fastingCheckInFollowUpReminderHours) },
                    { label: 'Dashboard layout JSON', value: this.text(currentUser.dashboardLayoutJson) },
                ],
            },
            {
                title: 'AI',
                fields: [
                    { label: 'Input token limit', value: this.number(currentUser.aiInputTokenLimit) },
                    { label: 'Output token limit', value: this.number(currentUser.aiOutputTokenLimit) },
                    { label: 'Consent accepted', value: this.dateTime(currentUser.aiConsentAcceptedAt) },
                ],
            },
            {
                title: 'Audit',
                fields: [
                    { label: 'Created', value: this.dateTime(currentUser.createdOnUtc) },
                    { label: 'Deleted', value: this.dateTime(currentUser.deletedAt) },
                    { label: 'Last login', value: this.dateTime(currentUser.lastLoginAtUtc) },
                    { label: 'Profile image asset ID', value: this.text(currentUser.profileImageAssetId) },
                ],
            },
        ];
    });

    public constructor() {
        this.loadDetails();
    }

    public close(): void {
        this.dialogRef.close(null);
    }

    public edit(): void {
        this.dialogRef.close('edit');
    }

    public impersonate(): void {
        this.dialogRef.close('impersonate');
    }

    private loadDetails(): void {
        this.usersService
            .getUser(this.initialUser.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: user => {
                    this.user.set(user);
                    this.hasError.set(false);
                    this.loadActivity(user.id);
                },
                error: () => {
                    this.hasError.set(true);
                    this.isLoading.set(false);
                },
            });
    }

    private loadActivity(userId: string): void {
        forkJoin({
            loginEvents: this.usersService.getLoginEvents(1, 3, null, userId),
            roleAuditEvents: this.usersService.getUserRoleAudit(userId),
        })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.loginEvents.set(response.loginEvents.items);
                    this.roleAuditEvents.set(response.roleAuditEvents);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.loginEvents.set([]);
                    this.roleAuditEvents.set([]);
                    this.isLoading.set(false);
                },
            });
    }

    public describeRoleActor(event: AdminUserRoleAuditEvent): string {
        if (event.actorEmail !== null && event.actorEmail !== undefined && event.actorEmail.trim().length > 0) {
            return event.actorEmail;
        }

        if (event.actorUserId !== null && event.actorUserId !== undefined && event.actorUserId.trim().length > 0) {
            return event.actorUserId;
        }

        return event.source;
    }

    private buildInitials(user: AdminUser): string {
        const first = user.firstName?.trim()[0] ?? '';
        const last = user.lastName?.trim()[0] ?? '';
        const value = `${first}${last}` || user.email.trim()[0] || '?';
        return value.toUpperCase();
    }

    private text(value: string | null | undefined): string {
        return value !== null && value !== undefined && value.trim().length > 0 ? value : '-';
    }

    private number(value: number | null | undefined): string {
        return value !== null && value !== undefined ? String(value) : '-';
    }

    private boolean(value: boolean | null | undefined): string {
        return value === true ? 'Yes' : 'No';
    }

    private date(value: string | null | undefined): string {
        return value !== null && value !== undefined ? new Date(value).toLocaleDateString() : '-';
    }

    private dateTime(value: string | null | undefined): string {
        return value !== null && value !== undefined ? new Date(value).toLocaleString() : '-';
    }
}
