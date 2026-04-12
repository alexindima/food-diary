export interface FastingStagePresentation {
    index: number;
    total: number;
    titleKey: string;
    descriptionKey: string;
    color: string;
    glowColor: string;
    nextTitleKey: string | null;
    nextInMs: number | null;
}

interface FastingStageDefinition {
    startsAtHours: number;
    titleKey: string;
    descriptionKey: string;
    color: string;
    glowColor: string;
}

const FASTING_STAGE_DEFINITIONS: readonly FastingStageDefinition[] = [
    {
        startsAtHours: 0,
        titleKey: 'FASTING.STAGES.EARLY.TITLE',
        descriptionKey: 'FASTING.STAGES.EARLY.DESCRIPTION',
        color: '#64748b',
        glowColor: 'rgba(100, 116, 139, 0.18)',
    },
    {
        startsAtHours: 4,
        titleKey: 'FASTING.STAGES.TRANSITION.TITLE',
        descriptionKey: 'FASTING.STAGES.TRANSITION.DESCRIPTION',
        color: '#2563eb',
        glowColor: 'rgba(37, 99, 235, 0.18)',
    },
    {
        startsAtHours: 12,
        titleKey: 'FASTING.STAGES.STORED_ENERGY.TITLE',
        descriptionKey: 'FASTING.STAGES.STORED_ENERGY.DESCRIPTION',
        color: '#7c3aed',
        glowColor: 'rgba(124, 58, 237, 0.2)',
    },
    {
        startsAtHours: 16,
        titleKey: 'FASTING.STAGES.DEEP.TITLE',
        descriptionKey: 'FASTING.STAGES.DEEP.DESCRIPTION',
        color: '#f59e0b',
        glowColor: 'rgba(245, 158, 11, 0.2)',
    },
] as const;

function getApplicableStages(plannedDurationHours: number): readonly FastingStageDefinition[] {
    const normalizedPlannedHours = Math.max(0, plannedDurationHours);
    const applicableStages = FASTING_STAGE_DEFINITIONS.filter(stage => stage.startsAtHours < normalizedPlannedHours);

    return applicableStages.length > 0 ? applicableStages : [FASTING_STAGE_DEFINITIONS[0]];
}

export function resolveFastingStage(elapsedMs: number, plannedDurationHours: number): FastingStagePresentation {
    const elapsedHours = Math.max(0, elapsedMs / 3_600_000);
    const applicableStages = getApplicableStages(plannedDurationHours);
    let currentIndex = 0;

    for (let index = applicableStages.length - 1; index >= 0; index--) {
        if (elapsedHours >= applicableStages[index].startsAtHours) {
            currentIndex = index;
            break;
        }
    }

    const currentStage = applicableStages[currentIndex];
    const nextStage = applicableStages[currentIndex + 1] ?? null;

    return {
        index: currentIndex + 1,
        total: applicableStages.length,
        titleKey: currentStage.titleKey,
        descriptionKey: currentStage.descriptionKey,
        color: currentStage.color,
        glowColor: currentStage.glowColor,
        nextTitleKey: nextStage?.titleKey ?? null,
        nextInMs: nextStage ? Math.max(0, nextStage.startsAtHours * 3_600_000 - elapsedMs) : null,
    };
}
