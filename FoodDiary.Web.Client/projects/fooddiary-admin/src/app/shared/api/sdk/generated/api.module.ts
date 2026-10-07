import { HttpClient } from '@angular/common/http';
import { ModuleWithProviders, NgModule, Optional, SkipSelf } from '@angular/core';
import { Configuration } from './configuration';

import { AdminAchievementsSdk } from './api/admin-achievements.service';
import { AdminAcquisitionSdk } from './api/admin-acquisition.service';
import { AdminAiPromptsSdk } from './api/admin-ai-prompts.service';
import { AdminAiUsageSdk } from './api/admin-ai-usage.service';
import { AdminAuditSdk } from './api/admin-audit.service';
import { AdminAuthSdk } from './api/admin-auth.service';
import { AdminBillingSdk } from './api/admin-billing.service';
import { AdminBugsSdk } from './api/admin-bugs.service';
import { AdminCatalogSdk } from './api/admin-catalog.service';
import { AdminDailyAdvicesSdk } from './api/admin-daily-advices.service';
import { AdminDashboardSdk } from './api/admin-dashboard.service';
import { AdminEmailTemplatesSdk } from './api/admin-email-templates.service';
import { AdminImagesSdk } from './api/admin-images.service';
import { AdminLessonsSdk } from './api/admin-lessons.service';
import { AdminMailInboxSdk } from './api/admin-mail-inbox.service';
import { AdminMealPlansSdk } from './api/admin-meal-plans.service';
import { AdminModerationSdk } from './api/admin-moderation.service';
import { AdminOutgoingEmailsSdk } from './api/admin-outgoing-emails.service';
import { AdminRetentionSdk } from './api/admin-retention.service';
import { AdminTelemetrySdk } from './api/admin-telemetry.service';
import { AdminUsersSdk } from './api/admin-users.service';

@NgModule({
    imports: [],
    declarations: [],
    exports: [],
    providers: [
        AdminAchievementsSdk,
        AdminAcquisitionSdk,
        AdminAiPromptsSdk,
        AdminAiUsageSdk,
        AdminAuditSdk,
        AdminAuthSdk,
        AdminBillingSdk,
        AdminBugsSdk,
        AdminCatalogSdk,
        AdminDailyAdvicesSdk,
        AdminDashboardSdk,
        AdminEmailTemplatesSdk,
        AdminImagesSdk,
        AdminLessonsSdk,
        AdminMailInboxSdk,
        AdminMealPlansSdk,
        AdminModerationSdk,
        AdminOutgoingEmailsSdk,
        AdminRetentionSdk,
        AdminTelemetrySdk,
        AdminUsersSdk,
    ],
})
export class ApiModule {
    public static forRoot(configurationFactory: () => Configuration): ModuleWithProviders<ApiModule> {
        return {
            ngModule: ApiModule,
            providers: [{ provide: Configuration, useFactory: configurationFactory }],
        };
    }

    constructor(@Optional() @SkipSelf() parentModule: ApiModule, @Optional() http: HttpClient) {
        if (parentModule) {
            throw new Error('ApiModule is already loaded. Import in your base AppModule only.');
        }
        if (!http) {
            throw new Error(
                'You need to import the HttpClientModule in your AppModule! \n' +
                    'See also https://github.com/angular/angular/issues/20575',
            );
        }
    }
}
