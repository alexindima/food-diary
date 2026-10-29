import { Service, signal } from '@angular/core';

/** A container that can reveal an otherwise hidden target without activating it. */
export type FdTourTargetResolver = {
    canReveal: (selector: string) => boolean;
    /** Update container state synchronously; the engine waits for Angular to render. */
    reveal: (selector: string) => () => void;
};

@Service()
export class FdTourTargetRegistry {
    private readonly resolversState = signal<readonly FdTourTargetResolver[]>([]);
    public readonly resolvers = this.resolversState.asReadonly();

    /** Register at the container boundary and call the disposer when it is destroyed. */
    public register(resolver: FdTourTargetResolver): () => void {
        this.resolversState.update(resolvers => [...resolvers, resolver]);
        return () => {
            this.resolversState.update(resolvers => resolvers.filter(candidate => candidate !== resolver));
        };
    }

    public find(selector: string): FdTourTargetResolver | undefined {
        return this.resolvers().find(resolver => resolver.canReveal(selector));
    }
}
