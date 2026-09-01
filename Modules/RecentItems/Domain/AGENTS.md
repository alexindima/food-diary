# RecentItems Domain

Own `RecentItem`, `RecentItemType` and `RecentItemId` with their legacy `FoodDiary.Domain` CLR namespaces. The one-way `RecentItem.User` navigation may reference central `User`/`UserId`; never add an inverse `User.RecentItems` navigation.
