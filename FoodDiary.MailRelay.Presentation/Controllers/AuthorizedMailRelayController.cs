using FoodDiary.MailRelay.Presentation.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Controllers;

[ServiceFilter(typeof(RelayApiKeyAuthorizationFilter))]
public abstract class AuthorizedMailRelayController(ISender sender) : MailRelayControllerBase(sender);
