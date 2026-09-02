# Users Domain Contracts

Own UserId and the public profile enum ActivityLevel with their existing CLR
namespaces and conversion semantics. Keep ActivityLevel in Enums with unchanged
names and numeric values; consumers reference this seam without acquiring User.
The only project dependency is shared Domain.Primitives for IEntityId.
Do not add aggregate, application or persistence dependencies. Moving ActivityLevel
from central Domain changes its assembly owner and requires coordinated rebuilds;
HTTP strings, EF string conversion and TDEE multipliers remain unchanged.
