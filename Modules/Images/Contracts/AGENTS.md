# Images Contracts

Own the dependency-free `ImageAssetId` compatibility contract under its stable
`FoodDiary.Domain.ValueObjects.Ids` CLR namespace. Central Domain may depend on
this project; never add a reference from Contracts back to central Domain or to
Images Domain.

Use canonical FoodDiary.Modules.Images project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
