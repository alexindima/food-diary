# Email persistence model

Own the provider-neutral email outbox record and its EF mapping. The shared
`FoodDiaryDbContext` applies this model explicitly. Email dispatch, jobs, transport
and generic outbox coordination remain outside this dependency-light model project.
