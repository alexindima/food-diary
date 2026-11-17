# MCP integration

Own the MCP resource endpoint, protocol configuration, per-tool permission checks and
retry handling. Web.Api composes this module and registers the module-owned tools.

Identity owns OAuth endpoints, grant issuance, and delegated session validation.
Recipes and MealPlanning own their tools' business commands and persisted results.
New tools should call the relevant owner use case and declare the narrow scope
required by that operation. Keep names, input schemas and response shapes compatible
with the live plugin when changing an existing tool.

Verify the public MCP and OAuth contracts in `RecipePluginOAuthTests`. The API host
integration suite covers registration, delegated authorization, and durable writes.
Add the project to the exact production reference matrix when changing dependencies.
