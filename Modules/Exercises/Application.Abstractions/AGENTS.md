# Exercises Application Abstractions

Own repository interfaces, persistence read models and ExerciseErrors. Reference Exercises Domain and Results only. Callers use ExerciseErrors directly; the central Errors.Exercise facade and central owner reference are retired. Never add the reverse central Abstractions reference.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
