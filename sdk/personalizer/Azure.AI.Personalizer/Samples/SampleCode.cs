// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Azure.AI.Personalizer
{
    public class SampleCode
    {
        public async Task SampelRankMethod()
        {
            string endpoint = "someUrl";
            string apiKey = "someKey";
            var options = new PersonalizerClientOptions();
            var credential = new AzureKeyCredential(apiKey);
            PersonalizerClient personalizerClient = new PersonalizerClient(new Uri(endpoint), credential, true, options: options, subsampleRate: 1.0f);
            IList<PersonalizerRankableAction> actions = new List<PersonalizerRankableAction>();
            actions.Add
                (new PersonalizerRankableAction(
                    id: "Person",
                    features:
                    new List<object>() { new { videoType = "documentary", videoLength = 35, director = "CarlSagan" }, new { mostWatchedByAge = "30-35" } }
            ));
            var request = new PersonalizerRankOptions(actions);
            // Action
            PersonalizerRankResult response = await personalizerClient.RankAsync(request);
        }

        public async Task SampelMultiSlotRankMethod()
        {
            string endpoint = "someUrl";
            string apiKey = "someKey";
            var options = new PersonalizerClientOptions();
            var credential = new AzureKeyCredential(apiKey);
            PersonalizerClient personalizerClient = new PersonalizerClient(new Uri(endpoint), credential, true, options: options, subsampleRate: 1.0f);
            IList<PersonalizerRankableAction> actions = new List<PersonalizerRankableAction>()
            {
                new PersonalizerRankableAction(
                        id: "NewsArticle",
                        features: new List<object>() { new { Type = "News" }}
                    ),
                new PersonalizerRankableAction(
                        id: "SportsArticle",
                        features: new List<object>() { new { Type = "Sports" }}
                    ),
                new PersonalizerRankableAction(
                        id: "EntertainmentArticle",
                        features: new List<object>() { new { Type = "Entertainment" }}
                )
            };

            PersonalizerSlotOptions slot1 = new PersonalizerSlotOptions(
                id: "Main Article",
                baselineAction: "NewsArticle",
                features: new List<object>()
                {
                    new
                    {
                        Size = "Large",
                        Position = "Top Middle"
                    }
                },
                excludedActions: new List<string>() { "SportsArticle", "EntertainmentArticle" }
                );

            PersonalizerSlotOptions slot2 = new PersonalizerSlotOptions(
                id: "Side Bar",
                baselineAction: "SportsArticle",
                features: new List<object>()
                {
                    new
                    {
                        Size = "Small",
                        Position = "Bottom Right"
                    }
                },
                excludedActions: new List<string>() { "EntertainmentArticle" }
                );

            IList<PersonalizerSlotOptions> slots = new List<PersonalizerSlotOptions>()
            {
                slot1,
                slot2
            };
            PersonalizerRankMultiSlotOptions request = new PersonalizerRankMultiSlotOptions(actions, slots);
            // Action
            PersonalizerMultiSlotRankResult response = await personalizerClient.RankMultiSlotAsync(request);
        }

        public async Task SampelRewardMethod()
        {
            string endpoint = "someUrl";
            string apiKey = "someKey";
            var options = new PersonalizerClientOptions();
            var credential = new AzureKeyCredential(apiKey);
            PersonalizerClient personalizerClient = new PersonalizerClient(new Uri(endpoint), credential, true, options: options, subsampleRate: 1.0f);

            // Action
            await personalizerClient.RewardAsync("someEventId", (float)0.8);
        }

        public async Task SampelActivateMethod()
        {
            string endpoint = "someUrl";
            string apiKey = "someKey";
            var options = new PersonalizerClientOptions();
            var credential = new AzureKeyCredential(apiKey);
            PersonalizerClient personalizerClient = new PersonalizerClient(new Uri(endpoint), credential, true, options: options, subsampleRate: 1.0f);

            // Action
            await personalizerClient.ActivateAsync("someEventId");
        }

        public async Task SampelModelMethod()
        {
            string endpoint = "someUrl";
            string apiKey = "someKey";
            var credential = new AzureKeyCredential(apiKey);
            var options = new PersonalizerClientOptions();
            PersonalizerAdministrationClient personalizerAdministrationClient = new PersonalizerAdministrationClient(new Uri(endpoint), credential, options);
            Response<Stream> response = await personalizerAdministrationClient.GetPersonalizerModelAsync(false);
            await personalizerAdministrationClient.ImportPersonalizerModelAsync(response.Value);
        }

    }
}
