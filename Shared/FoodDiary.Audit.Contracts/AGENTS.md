# Shared audit contracts

Own the dependency-light structured audit read/write/logging contracts shared by
multiple modules and implemented by the central audit store. Do not add EF,
transport, module workflows or provider code here. The UserId dependency is an
explicit actor-identity seam to Users Domain.Contracts.
