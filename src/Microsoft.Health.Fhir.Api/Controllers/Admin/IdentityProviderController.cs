// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Api.Features.Security;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Messages.Delete;
using Microsoft.Health.Fhir.Core.Messages.Get;
using Microsoft.Health.Fhir.Core.Messages.Upsert;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Fhir.Api.Controllers.Admin
{
    [Route("Admin/IdentityProvider/")]
    public class IdentityProviderController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<IdentityProviderController> _logger;
        private readonly SchemeManager _schemeManager;

        public IdentityProviderController(
            IMediator mediator,
            ILogger<IdentityProviderController> logger,
            SchemeManager schemeManager)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(schemeManager, nameof(schemeManager));

            _mediator = mediator;
            _logger = logger;
            _schemeManager = schemeManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var response = await _mediator.Send<GetAllIdentityProvidersResponse>(new GetAllIdentityProvidersRequest(), cancellationToken);
            return Ok(response.Outcome);
        }

        [HttpGet]
        [Route("{IdentityProviderName}")]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string identityProviderName, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send<GetIdentityProviderResponse>(new GetIdentityProviderRequest(identityProviderName), cancellationToken);
            return Ok(response.Outcome);
        }

        [HttpPut]
        [Route("{IdentityProviderName}")]
        [AllowAnonymous]
        public async Task<IActionResult> Update(string identityProviderName, [FromBody] IdentityProvider identityProvider, CancellationToken cancellationToken)
        {
            if (identityProviderName != identityProvider.Name)
            {
                return Conflict();
            }

            var suppliedWeakETag = HttpContext.Request.Headers[HeaderNames.IfMatch];

            WeakETag weakETag = null;
            if (!string.IsNullOrWhiteSpace(suppliedWeakETag))
            {
                weakETag = WeakETag.FromWeakETag(suppliedWeakETag);
            }

            var response = await _mediator.Send<UpsertIdentityProviderResponse>(new UpsertIdentityProviderRequest(identityProvider, weakETag), cancellationToken);

            _schemeManager.AddScheme(response.Outcome);

            return Ok(response.Outcome);
        }

        [HttpDelete]
        [Route("{IdentityProviderName}")]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(string identityProviderName, CancellationToken cancellationToken)
        {
            await _mediator.Send<DeleteIdentityProviderResponse>(new DeleteIdentityProviderRequest(identityProviderName), cancellationToken);

            _schemeManager.RemoveScheme(identityProviderName);
            return Ok();
        }
    }
}
