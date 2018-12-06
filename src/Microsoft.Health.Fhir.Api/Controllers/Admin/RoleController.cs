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
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Messages.Delete;
using Microsoft.Health.Fhir.Core.Messages.Get;
using Microsoft.Health.Fhir.Core.Messages.Upsert;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Fhir.Api.Controllers.Admin
{
    [Route("Admin/Role/")]
    public class RoleController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IMediator mediator,
            ILogger<RoleController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var response = await _mediator.Send<GetAllRolesResponse>(new GetAllRolesRequest(), cancellationToken);
            return Ok(response.Outcome);
        }

        [HttpGet]
        [Route("{roleName}")]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string roleName, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send<GetRoleResponse>(new GetRoleRequest(roleName), cancellationToken);
            return Ok(response.Outcome);
        }

        [HttpPut]
        [Route("{roleName}")]
        [AllowAnonymous]
        public async Task<IActionResult> Update(string roleName, [FromBody] Role role, CancellationToken cancellationToken)
        {
            if (roleName != role.Name)
            {
                return Conflict();
            }

            var suppliedWeakETag = HttpContext.Request.Headers[HeaderNames.IfMatch];

            WeakETag weakETag = null;
            if (!string.IsNullOrWhiteSpace(suppliedWeakETag))
            {
                weakETag = WeakETag.FromWeakETag(suppliedWeakETag);
            }

            var response = await _mediator.Send<UpsertRoleResponse>(new UpsertRoleRequest(role, weakETag), cancellationToken);

            return Ok(response.Outcome);
        }

        [HttpDelete]
        [Route("{roleName}")]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(string roleName, CancellationToken cancellationToken)
        {
            await _mediator.Send<DeleteRoleResponse>(new DeleteRoleRequest(roleName), cancellationToken);

            return Ok();
        }
    }
}
