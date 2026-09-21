import { spawnSync } from 'node:child_process';

const zones = [
    'UTC',
    'Asia/Tbilisi',
    'America/Los_Angeles',
    'America/St_Johns',
    'Asia/Kathmandu',
    'Pacific/Kiritimati',
    'Pacific/Pago_Pago',
    'Europe/Berlin',
    'Australia/Lord_Howe',
];
for (const zone of zones) {
    console.log(`Measurement and dashboard calendar tests: ${zone}`);
    const result = spawnSync(
        process.execPath,
        [
            'node_modules/@angular/cli/bin/ng.js',
            'test',
            'food-diary-web-client',
            '--watch=false',
            '--include=**/measurement-calendar-timezone.spec.ts',
            '--include=**/measurement-history-pager.spec.ts',
            '--include=**/dashboard-calendar-timezone.spec.ts',
        ],
        { stdio: 'inherit', env: { ...process.env, TZ: zone } },
    );
    if (result.error) throw result.error;
    if (result.status !== 0) process.exit(result.status ?? 1);
}
