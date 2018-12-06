// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Messages.Get;

namespace Microsoft.Health.Fhir.Api.Features.Security
{
    public class SchemeManager
    {
        private IssuerSchemeMapper _issuerSchemeMapper;
        private IMediator _mediator;
        private IAuthenticationSchemeProvider _schemeProvider;
        private IPostConfigureOptions<JwtBearerOptions> _postConfigureOptions;
        private IOptionsMonitorCache<JwtBearerOptions> _optionsMonitorCache;

        public SchemeManager(IssuerSchemeMapper issuerSchemeMapper, IMediator mediator, IAuthenticationSchemeProvider schemeProvider, IPostConfigureOptions<JwtBearerOptions> postConfigureOptions, IOptionsMonitorCache<JwtBearerOptions> optionsMonitorCache)
        {
            _issuerSchemeMapper = issuerSchemeMapper;
            _mediator = mediator;
            _schemeProvider = schemeProvider;
            _postConfigureOptions = postConfigureOptions;
            _optionsMonitorCache = optionsMonitorCache;
        }

        public void SetupSchemes()
        {
                var identityProviders = _mediator
                    .Send<GetAllIdentityProvidersResponse>(new GetAllIdentityProvidersRequest())
                    .Result.Outcome;

                foreach (var identityProvider in identityProviders)
                {
                    AddScheme(identityProvider);
                }
        }

        public void AddScheme(IdentityProvider identityProvider)
        {
            _schemeProvider.AddScheme(new AuthenticationScheme(
                identityProvider.Name,
                identityProvider.Name,
                typeof(JwtBearerHandler)));

            _issuerSchemeMapper.AuthorityToSchemeMap.Add(identityProvider.Authority, identityProvider.Name);
            var options = new JwtBearerOptions
            {
                Authority = identityProvider.Authority,
                Audience = identityProvider.Audience,
            };
            _postConfigureOptions.PostConfigure(identityProvider.Name, options);

            _optionsMonitorCache.TryAdd(identityProvider.Name, options);
        }

        public void RemoveScheme(string name)
        {
            _schemeProvider.RemoveScheme(name);

            foreach (var item in _issuerSchemeMapper.AuthorityToSchemeMap.Where(x => x.Value == name).ToList())
            {
                _issuerSchemeMapper.AuthorityToSchemeMap.Remove(item.Key);
            }

            _optionsMonitorCache.TryRemove(name);
        }
    }
}
