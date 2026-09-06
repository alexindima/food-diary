# Ai module guidelines

Own Ai ports, errors, models and AiUsage administration projections. Keep legacy CLR namespaces. Depend only on Ai Domain and shared Results; never central Application Abstractions (which must not re-export this project).

Ai/Common owns IAiAdministrationReadService and IAiPromptAdministrationService in the matching Abstractions namespace. Administration writes return AiPromptTemplateReadModel; never return the mutable AiPromptTemplate to Admin. Implementations remain in Ai Application.
