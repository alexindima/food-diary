const VARIABLES: Readonly<Record<string, readonly string[]>> = {
    email_verification: ['brand', 'link'],
    password_reset: ['brand', 'link'],
    account_created: ['brand', 'email', 'temporaryPassword', 'loginLink', 'link'],
    dietologist_invitation: ['brand', 'clientName', 'link'],
    bug_report_received: ['brand'],
};

export function emailTemplateVariables(key: string): readonly string[] {
    return VARIABLES[key.trim().toLowerCase()] ?? [];
}
