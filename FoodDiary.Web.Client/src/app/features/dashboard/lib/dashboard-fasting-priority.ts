import type { FastingSession } from '../../fasting/models/fasting.data';
import { buildDashboardFastingTimeline } from '../components/dashboard-fasting-card/dashboard-fasting-timeline';

export function shouldPrioritizeDashboardFasting(session: FastingSession | null, now: number): boolean {
    if (session?.endedAtUtc !== null) {
        return false;
    }
    const start = new Date(session.startedAtUtc).getTime();
    if (!Number.isFinite(start) || start > now) {
        return false;
    }
    return !buildDashboardFastingTimeline(session, now - start).eating;
}
