/*
  Warnings:

  - You are about to drop the `RecipeItem` table. If the table is not empty, all the data it contains will be lost.
  - You are about to drop the `RecipeStepItem` table. If the table is not empty, all the data it contains will be lost.

*/
-- DropForeignKey
ALTER TABLE "RecipeItem" DROP CONSTRAINT "RecipeItem_foodId_fkey";

-- DropForeignKey
ALTER TABLE "RecipeItem" DROP CONSTRAINT "RecipeItem_recipeId_fkey";

-- DropForeignKey
ALTER TABLE "RecipeStepItem" DROP CONSTRAINT "RecipeStepItem_recipeItemId_fkey";

-- DropForeignKey
ALTER TABLE "RecipeStepItem" DROP CONSTRAINT "RecipeStepItem_stepId_fkey";

-- DropTable
DROP TABLE "RecipeItem";

-- DropTable
DROP TABLE "RecipeStepItem";

-- CreateTable
CREATE TABLE "RecipeIngredient" (
    "id" SERIAL NOT NULL,
    "stepId" INTEGER NOT NULL,
    "foodId" INTEGER NOT NULL,
    "amount" DOUBLE PRECISION NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "RecipeIngredient_pkey" PRIMARY KEY ("id")
);

-- AddForeignKey
ALTER TABLE "RecipeIngredient" ADD CONSTRAINT "RecipeIngredient_stepId_fkey" FOREIGN KEY ("stepId") REFERENCES "RecipeStep"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "RecipeIngredient" ADD CONSTRAINT "RecipeIngredient_foodId_fkey" FOREIGN KEY ("foodId") REFERENCES "Food"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
