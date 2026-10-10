import type { CycleEpisodeFacade } from "@candidate/app/features/cycle-tracking/lib/cycle-episode.facade";
import type { CycleTrackingStateFacade } from "@candidate/app/features/cycle-tracking/lib/cycle-tracking-state.facade";
import { entityId } from "@candidate/app/shared/models/semantics/entity-id";

declare const episodes: CycleEpisodeFacade;
declare const state: CycleTrackingStateFacade;
const episode = entityId<"menstrual-episode">("owned");
const recipe = entityId<"recipe">("foreign");
episodes.editMenstrualEpisode(episode);
void episodes.deleteMenstrualEpisodeAsync(episode);
state.editingEpisodeId.set(episode);
// @ts-expect-error Recipe identity cannot edit a menstrual episode.
episodes.editMenstrualEpisode(recipe);
// @ts-expect-error Raw strings must not reach destructive actions.
void episodes.deleteMenstrualEpisodeAsync("raw");
// @ts-expect-error Busy state must keep episode ownership.
state.deletingEpisodeId.set(recipe);
