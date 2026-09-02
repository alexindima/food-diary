# Users domain contracts

Own UserId, ActivityLevel and LanguageCode with their existing CLR namespaces. Reference only shared Domain.Primitives. LanguageCode owns the user's en/ru preference contract: TryParse accepts only exact normalized codes; FromPreferred maps the ru prefix to ru and otherwise defaults to en. Preserve this behavior and all existing ID/enum semantics. Do not add aggregate, application or persistence dependencies.
