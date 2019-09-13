// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Messages.Search;

namespace Microsoft.Health.Fhir.Shared.Core.Features.Subscriptions
{
    public class TopicStore
    {
        private readonly Dictionary<string, Topic> _topicsById;
        private readonly Dictionary<string, Dictionary<string, Topic>> _topicsByResourceType;

        public TopicStore()
        {
            _topicsById = new Dictionary<string, Topic>();
            _topicsByResourceType = new Dictionary<string, Dictionary<string, Topic>>();
        }

        public bool IsInitialized { get; private set; }

        public IReadOnlyList<Topic> GetForResourceType(string resourceType)
        {
            if (_topicsByResourceType.ContainsKey(resourceType))
            {
                return _topicsByResourceType[resourceType].Select(x => x.Value).ToList();
            }

            return new List<Topic>();
        }

        public void AddTopic(Topic topic)
        {
            string topicId = topic.Id;

            if (_topicsById.ContainsKey(topicId))
            {
                _topicsById[topicId] = topic;
            }
            else
            {
                _topicsById.Add(topicId, topic);
            }

            foreach (var resourceType in topic.ResourceTrigger.ResourceType)
            {
                string resourceTypeString = resourceType.ToString();
                if (_topicsByResourceType.ContainsKey(resourceTypeString))
                {
                    if (_topicsByResourceType[resourceTypeString].ContainsKey(topicId))
                    {
                        _topicsByResourceType[resourceTypeString][topicId] = topic;
                    }
                    else
                    {
                        _topicsByResourceType[resourceTypeString].Add(topicId, topic);
                    }
                }
                else
                {
                    _topicsByResourceType.Add(resourceTypeString, new Dictionary<string, Topic> { { topicId, topic } });
                }
            }
        }

        public void Start(IMediator mediator)
        {
            IsInitialized = true;
            var result = mediator.Send(new SearchResourceRequest("Topic", new List<Tuple<string, string>>())).GetAwaiter().GetResult();

            var topics = result.Bundle.ToPoco<Bundle>();

            do
            {
                foreach (Bundle.EntryComponent bundleEntry in topics.Entry)
                {
                    AddTopic((Topic)bundleEntry.Resource);
                }

                if (topics.NextLink != null)
                {
                    // TODO: BKP - Figure out how to do the continuation to get them all
                    topics.NextLink = null;
                }
            }
            while (topics.NextLink != null);
        }
    }
}
