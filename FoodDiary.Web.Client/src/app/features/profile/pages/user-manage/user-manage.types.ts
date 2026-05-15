import type { WebPushSubscriptionItem } from '../../../../services/notification.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import type { DietologistPermissions } from '../../../../shared/models/dietologist.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import type { ActivityLevelOption, Gender, UiStyleOption } from '../../../../shared/models/user.data';
import type { AppThemeName } from '../../../../theme/app-theme.config';
import type { BillingOverview } from '../../../premium/models/billing.models';

export type UserFormValues = {
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    email: string | null;
    birthDate: string | null;
    gender: Gender | null;
    language: string | null;
    theme: AppThemeName | null;
    uiStyle: UiStyleOption | null;
    height: number | null;
    activityLevel: ActivityLevelOption | null;
    stepGoal: number | null;
    profileImage: ImageSelection | null;
    pushNotificationsEnabled?: boolean | null;
    fastingPushNotificationsEnabled?: boolean | null;
    socialPushNotificationsEnabled?: boolean | null;
};

export type UserFormData = FormGroupControls<UserFormValues>;

type DietologistFormValues = {
    email: string;
    shareProfile: boolean;
    shareMeals: boolean;
    shareStatistics: boolean;
    shareWeight: boolean;
    shareWaist: boolean;
    shareGoals: boolean;
    shareHydration: boolean;
    shareFasting: boolean;
};

export type DietologistFormData = FormGroupControls<DietologistFormValues>;
export type DietologistPermissionControlName = Exclude<keyof DietologistFormValues, 'email'>;

export type DietologistPermissionChange = {
    controlName: keyof DietologistPermissions;
    value: boolean;
};

export type ConnectedDeviceViewModel = {
    subscription: WebPushSubscriptionItem;
    label: string;
    meta: string;
    isCurrent: boolean;
};

export type BillingViewModel = {
    overview: BillingOverview;
    statusTone: 'success' | 'muted';
    endLabelKey: string;
    showNextAttempt: boolean;
    premiumActionVariant: 'secondary' | 'primary';
    premiumActionLabelKey: string;
    showManagedSupportNote: boolean;
};

export type ProfileStatusViewModel = {
    key: string;
    tone: 'success' | 'warning' | 'danger' | 'muted';
};
