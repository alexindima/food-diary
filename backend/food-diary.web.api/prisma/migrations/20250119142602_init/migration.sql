/*
  Warnings:

  - A unique constraint covering the columns `[recipeId,foodId]` on the table `RecipeItem` will be added. If there are existing duplicate values, this will fail.

*/
-- CreateIndex
CREATE UNIQUE INDEX "RecipeItem_recipeId_foodId_key" ON "RecipeItem"("recipeId", "foodId");
