# Images Contracts

Own the dependency-free `ImageAssetId` compatibility contract under its stable
`FoodDiary.Domain.ValueObjects.Ids` CLR namespace. Central Domain may depend on
this project; never add a reference from Contracts back to central Domain or to
Images Domain.
