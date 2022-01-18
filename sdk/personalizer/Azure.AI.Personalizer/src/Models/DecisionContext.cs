// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Personalizer
{
    /// <summary> The Decision Context. </summary>
    public class DecisionContext
    {
        /// <summary> The Decision Context. </summary>
        public DecisionContext()
        {
        }

        /// <summary> Initializes a new instance of DecisionContext. </summary>
        /// <param name="rankRequest"> Personalizer Rank Options </param>
        public DecisionContext(PersonalizerRankOptions rankRequest)
        {
            List<string> jsonFeatures = rankRequest.ContextFeatures.Select(f => JsonConvert.SerializeObject(f)).ToList();
            this.SharedFromUrl = jsonFeatures;

            this.Documents = rankRequest.Actions
                .Select(action =>
                {
                    List<string> jsonFeatures = action.Features.Select(f => JsonConvert.SerializeObject(f)).ToList();

                    var doc = new DecisionContextDocument
                    {
                        ID = action.Id,
                        JSON = jsonFeatures,
                    };

                    return doc;
                }).ToArray();
        }

        /// <summary> Properties from url </summary>
        [JsonProperty("FromUrl", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(JsonRawStringListConverter))]
        public List<string> SharedFromUrl { get; set; }

        /// <summary> Properties of documents </summary>
        [JsonProperty("_multi")]
        public DecisionContextDocument[] Documents { get; set; }

        /// <summary> Properties of slots </summary>
        [JsonProperty("_slots", NullValueHandling = NullValueHandling.Ignore)]
        public DecisionContextDocument[] Slots { get; set; }
    }
}
