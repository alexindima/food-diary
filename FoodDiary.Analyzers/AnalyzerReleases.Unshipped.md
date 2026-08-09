; Unshipped analyzer release

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
FD0001 | Style | Disabled | Require C# 14 extension blocks in migrated scopes
FD0002 | Style | Disabled | Require Async suffixes on asynchronous methods
FD0003 | Style | Disabled | Reject Async suffixes on synchronous methods
FD0004 | Style | Disabled | Require CancellationToken on asynchronous methods
FD0005 | Style | Disabled | Reject target-typed new in invocation arguments
FD0006 | Style | Disabled | Require TimeProvider instead of direct UtcNow access
FD0007 | Style | Disabled | Require coverage exclusion on test types
FD0008 | Style | Disabled | Require concrete classes to be closed for inheritance in governed scopes
FD0009 | Reliability | Disabled | Reject direct test connections to literal external hosts
FD0010 | Reliability | Disabled | Reject span overloads inside expression trees
FD0011 | Style | Disabled | Reject redundant empty record parameter lists
FD0012 | Style | Disabled | Reject private readonly fields that only store primary constructor parameters
