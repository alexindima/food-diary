// Keep failed-run reports for diagnosis; Angular assigns a directory per project.
export default {
    test: {
        maxWorkers: 4,
        coverage: {
            reportOnFailure: true,
            thresholds: {
                'src/app/features/meals/**/*.ts': { lines: 93, statements: 93, functions: 93, branches: 84 },
                'src/app/features/weight-history/**/*.ts': { lines: 99, statements: 99, functions: 100, branches: 97 },
                'src/app/features/waist-history/**/*.ts': { lines: 99, statements: 99, functions: 100, branches: 97 },
                'src/app/features/dashboard/lib/dashboard.facade.ts': { lines: 95, statements: 95, functions: 95, branches: 95 },
                'src/app/features/dashboard/api/dashboard.service.ts': { lines: 100, statements: 100, functions: 100, branches: 100 },
                'src/app/features/dashboard/lib/dashboard-layout.service.ts': { lines: 95, statements: 95, functions: 95, branches: 80 },
                'src/app/features/dashboard/dialogs/dashboard-notification-settings-dialog/dashboard-notification-settings.facade.ts': {
                    lines: 95,
                    statements: 95,
                    functions: 95,
                    branches: 95,
                },
                'src/app/features/weekly-check-in/lib/weekly-check-in.facade.ts': {
                    lines: 95,
                    statements: 95,
                    functions: 95,
                    branches: 90,
                },
                'src/app/features/fasting/lib/fasting.facade.ts': { lines: 95, statements: 95, functions: 95, branches: 80 },
                'src/app/shared/api/user.service.ts': { lines: 95, statements: 95, functions: 95, branches: 75 },
            },
            reporter: ['text-summary', 'html', 'json', 'json-summary', 'lcov'],
        },
    },
};
