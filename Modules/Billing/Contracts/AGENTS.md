# Billing consumer contracts

Own RenewDueSubscriptionsCommand and BillingRenewalRunResult for scheduler dispatch, plus IBillingMarketingConversionRecorder implemented by Marketing. Depend only on the narrow shared Mediator project. Keep provider models, repositories, handlers and aggregate writes with their owners.

RenewDueSubscriptionsCommand is IRequest<T>, not the auto-saving ICommand<T>. The handler retains explicit short transactions and calls the payment provider outside replayable callbacks.
