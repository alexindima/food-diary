# Products Infrastructure

Own Product repositories, cache wrapper, read adapters and complete DI. Depend on central Infrastructure for shared context and internal composition lock. Do not add SaveChanges outside the existing transaction runner. Preserve scoped aliases and SQL. Central Infrastructure must never reference this adapter project.
