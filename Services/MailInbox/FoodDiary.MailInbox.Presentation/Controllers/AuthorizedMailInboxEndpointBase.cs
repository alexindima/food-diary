using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Controllers;

[ServiceFilter(typeof(MailInboxApiKeyAuthorizationFilter))]
public abstract class AuthorizedMailInboxEndpointBase(ISender sender) : MailInboxControllerBase(sender);
