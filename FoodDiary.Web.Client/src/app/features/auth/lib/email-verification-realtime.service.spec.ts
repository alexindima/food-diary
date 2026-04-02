import { toEmailVerificationHubUrl } from './email-verification-realtime.service';

describe('toEmailVerificationHubUrl', () => {
    it('should build the email verification hub URL from a versioned auth API base URL', () => {
        expect(toEmailVerificationHubUrl('https://fooddiary.club/api/v1/auth')).toBe('https://fooddiary.club/hubs/email-verification');
    });

    it('should build the email verification hub URL from a non-versioned auth API base URL', () => {
        expect(toEmailVerificationHubUrl('http://localhost:5300/api/auth')).toBe('http://localhost:5300/hubs/email-verification');
    });
});
