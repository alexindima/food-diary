# Outbox persistence model

Own the shared operator replay-audit record and its EF mapping. Generic claiming,
processing and replay coordination stay in the central Infrastructure runtime;
stream records and SQL remain with Email or the owning feature module.
