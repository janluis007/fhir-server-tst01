// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Api.Features.Patch.FhirPatch
{
    internal class JsonPointer : IPathProvider
    {
        public IEnumerable<JToken> Select(JToken source, string path)
        {
            string[] tokens = path.Split('/').Skip(1).Select(Decode).ToArray();

            return new[] { Get(source, tokens) };
        }

        public void Set(JToken source, string path, JToken value)
        {
            string[] tokens = path.Split('/').Skip(1).Select(Decode).ToArray();

            JToken property = Find(source, tokens, true);
            property.Replace(value);
        }

        private JToken Get(JToken source, IReadOnlyCollection<string> tokens)
        {
            return Find(source, tokens, false);
        }

        private JToken Find(JToken source, IReadOnlyCollection<string> tokens, bool create)
        {
            if (tokens.Count == 0)
            {
                return source;
            }

            try
            {
                JToken pointer = source;
                foreach (var token in tokens)
                {
                    if (pointer is JArray array)
                    {
                        switch (token)
                        {
                            case "-":
                                pointer = JToken.FromObject(default(int));
                                array.Add(pointer);
                                break;
                            case "":
                                break;
                            default:
                                pointer = pointer[Convert.ToInt32(token)];
                                break;
                        }
                    }
                    else if (token == "")
                    {
                        pointer = pointer.Value<JToken>();
                    }
                    else
                    {
                        JToken temp = pointer[token];
                        if (temp == null)
                        {
                            if (create)
                            {
                                pointer[token] = JToken.FromObject(default(int));
                                pointer = pointer[token];
                            }
                            else
                            {
                                throw new ArgumentException("Cannot find " + token);
                            }
                        }
                        else
                        {
                            pointer = temp;
                        }
                    }
                }

                return pointer;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Failed to dereference pointer", ex);
            }
        }

        private string Decode(string token)
        {
            return Uri.UnescapeDataString(token)
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
        }
    }
}
