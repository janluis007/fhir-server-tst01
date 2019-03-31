// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace SandboxImporter
{
    public class UrlHelperFactory : IUrlHelperFactory
    {
        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            return new UriHelper();
        }

        private class UriHelper : IUrlHelper
        {
            public ActionContext ActionContext
            {
                get { throw new NotSupportedException(); }
            }

            public string Action(UrlActionContext actionContext)
            {
                throw new NotImplementedException();
            }

            public string Content(string contentPath)
            {
                throw new NotImplementedException();
            }

#pragma warning disable CA1054 // Uri parameters should not be strings
            public bool IsLocalUrl(string url)
#pragma warning restore CA1054 // Uri parameters should not be strings
            {
                throw new NotImplementedException();
            }

#pragma warning disable CA1055 // Uri return values should not be strings
            public string RouteUrl(UrlRouteContext routeContext)
#pragma warning restore CA1055 // Uri return values should not be strings
            {
                return $"https://localhost/{routeContext.RouteName}";
            }

            public string Link(string routeName, object values)
            {
                throw new NotImplementedException();
            }
        }
    }
}
