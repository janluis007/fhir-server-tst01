// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;
using EnsureThat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Api.Configs;
using Microsoft.Health.Fhir.Api.Features.Security;
using Microsoft.Health.Fhir.Api.Features.Security.Authorization;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Security.Authorization;

namespace Microsoft.Health.Fhir.Api.Modules
{
    public class SecurityModule : IStartupModule
    {
        private readonly SecurityConfiguration _securityConfiguration;

        public SecurityModule(FhirServerConfiguration fhirServerConfiguration)
        {
            EnsureArg.IsNotNull(fhirServerConfiguration, nameof(fhirServerConfiguration));

            _securityConfiguration = fhirServerConfiguration.Security;
        }

        /// <inheritdoc />
        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            var issuerSchemeMapper = new IssuerSchemeMapper();
            services.AddSingleton(issuerSchemeMapper);
            services.AddTransient<SchemeManager>();
            services.AddTransient<IStartupFilter, SecurityModuleStartupFilter>();
            services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

            // Set the token handler to not do auto inbound mapping. (e.g. "roles" -> "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            if (_securityConfiguration.Enabled)
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "custom";
                        options.DefaultChallengeScheme = "custom";
                        options.DefaultScheme = "custom";
                    })
                    .AddPolicyScheme("custom", "Bearer or Secret Header", options =>
                    {
                        options.ForwardDefaultSelector = context =>
                        {
                            var authorization = context.Request.Headers["Authorization"].ToString();

                            var handler = new JwtSecurityTokenHandler();

                            // If no authorization header found, nothing to process further
                            if (string.IsNullOrEmpty(authorization))
                            {
                                return "defaultJwt";
                            }

                            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                var token = authorization.Substring("Bearer ".Length).Trim();
                                var jwtToken = handler.ReadJwtToken(token);
                                if (issuerSchemeMapper.AuthorityToSchemeMap.TryGetValue(jwtToken.Issuer, out string scheme))
                                {
                                    return scheme;
                                }
                            }

                            return "defaultJwt";
                        };
                    })
                    .AddJwtBearer("defaultJwt", options => { });

                services.AddAuthorization(options => options.AddPolicy(PolicyNames.FhirPolicy, builder =>
                {
                    builder.RequireAuthenticatedUser();
                    builder.Requirements.Add(new FhirAccessRequirement());
                }));

                services.AddSingleton<IAuthorizationHandler, DefaultFhirAccessRequirementHandler>();

                if (_securityConfiguration.Authorization.Enabled)
                {
                    ////_securityConfiguration.Authorization.ValidateRoles();
                    services.AddSingleton(_securityConfiguration.Authorization);
                    services.AddScoped<IAuthorizationPolicy, RoleBasedAuthorizationPolicy>();
                    services.AddScoped<IAuthorizationHandler, ResourceActionHandler>();
                }
                else
                {
                    services.AddAuthorization(options => ConfigureDefaultPolicy(options, PolicyNames.HardDeletePolicy, PolicyNames.ReadPolicy, PolicyNames.WritePolicy));
                }
            }
            else
            {
                services.AddAuthorization(options => ConfigureDefaultPolicy(options, PolicyNames.FhirPolicy, PolicyNames.HardDeletePolicy, PolicyNames.ReadPolicy, PolicyNames.WritePolicy));
            }
        }

        private static void ConfigureDefaultPolicy(AuthorizationOptions options, params string[] policyNames)
        {
            foreach (var policyName in policyNames)
            {
                options.AddPolicy(policyName, builder => builder.RequireAssertion(x => true));
            }
        }

        private class SecurityModuleStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    var scopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

                    using (var scope = scopeFactory.CreateScope())
                    {
                        var schemeManager = scope.ServiceProvider.GetRequiredService<SchemeManager>();

                        schemeManager.SetupSchemes();
                    }

                    next(app);
                };
            }
        }
    }
}
