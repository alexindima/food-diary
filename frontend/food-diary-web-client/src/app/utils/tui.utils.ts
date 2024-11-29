import { TuiDay, TuiTime } from '@taiga-ui/cdk';

export class TuiUtils {
    public static combineTuiDayAndTuiTime(day: TuiDay, time: TuiTime): Date {
        const date = day.toLocalNativeDate();

        date.setHours(time.hours, time.minutes, time.seconds || 0, time.ms || 0);

        return date;
    }
}
