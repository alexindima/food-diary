# Products domain contracts

Own ProductId, ProductType and MeasurementUnit with stable FoodDiary.Domain.* namespaces and numeric values. Reference only shared Domain.Primitives. Product aggregates and food-quality algorithms belong to Products Domain; never reference that aggregate-bearing assembly from this contract seam.
