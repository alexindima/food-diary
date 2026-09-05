# MailRelay email adapter

This project owns the primary FoodDiary application's adapter from the shared
`IEmailTransport` contract to the approved MailRelay client package. Keep rendered
email policy and persistence outside this assembly. Preserve timeout, configuration,
cancellation and transport error semantics; never reference MailRelay server layers.
