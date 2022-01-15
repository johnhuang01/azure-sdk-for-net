// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Rl.Net;

namespace Azure.AI.Personalizer
{
    /// <summary> The Rank Processor. </summary>
    internal class RankProcessor
    {
        private readonly LiveModel _liveModel;
        internal PolicyRestClient RestClient { get; }

        /// <summary> Initializes a new instance of RankProcessor. </summary>
        public RankProcessor(LiveModel liveModel)
        {
            this._liveModel = liveModel;
        }

        /// <summary> Submit a Personalizer rank request. Receives a context and a list of actions. Returns which of the provided actions should be used by your application, in rewardActionId. </summary>
        /// <param name="options"> A Personalizer Rank request. </param>
        public Response<PersonalizerRankResult> Rank(PersonalizerRankOptions options)
        {
            if (String.IsNullOrEmpty(options.EventId))
            {
                options.EventId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            }

            HashSet<string> excludedSet = new HashSet<string>(options.ExcludedActions);

            // Store the original action list
            List<PersonalizerRankableAction> originalActions = new List<PersonalizerRankableAction>();
            List<PersonalizerRankableAction> rankableActions = new List<PersonalizerRankableAction>();
            List<PersonalizerRankableAction> excludedActions = new List<PersonalizerRankableAction>();
            int idx = 0;
            foreach (var action in options.Actions)
            {
                action.Index = idx;
                originalActions.Add(action);
                if (excludedSet.Contains(action.Id))
                {
                    excludedActions.Add(action);
                }
                else
                {
                    rankableActions.Add(action);
                }
                ++idx;
            }

            // Remove excluded actions in options
            options.Actions = options.Actions.Where(action => !excludedSet.Contains(action.Id));

            // Convert options to the compatible parameter for ChooseRank
            DecisionContext decisionContext = new DecisionContext(options);
            var contextJson = JsonConvert.SerializeObject(decisionContext);
            ActionFlags flags = options.DeferActivation == true ? ActionFlags.Deferred : ActionFlags.Default;

            // Call ChooseRank of local RL.Net
            RankingResponse rankingResponse = _liveModel.ChooseRank(options.EventId, contextJson, flags);

            // Convert response to PersonalizerRankResult
             var value = GenerateRankResult(originalActions, rankableActions, excludedActions, rankingResponse, options.EventId);

            return Response.FromValue(value, default);
        }

        public static PersonalizerRankResult GenerateRankResult(List<PersonalizerRankableAction> originalActions,
            List<PersonalizerRankableAction> rankableActions, List<PersonalizerRankableAction> excludedActions, RankingResponse rankingResponse, string eventId)
        {
            var rankedIndices = rankingResponse?.Select(actionProbability => ((int)actionProbability.ActionIndex + 1)).ToArray();

            var rankingProbabilities = rankingResponse?.Select(actionProbability =>
                actionProbability.Probability).ToArray();

            return GenerateRankResponse(originalActions, rankableActions, excludedActions, rankedIndices, rankingProbabilities, eventId);
        }

        public static PersonalizerRankResult GenerateRankResponse(List<PersonalizerRankableAction> originalActions,
            List<PersonalizerRankableAction> rankableActions, List<PersonalizerRankableAction> excludedActions, int[] rankedIndices, float[] rankingProbabilities, string eventId, int multiSlotChosenActionIndex = -1)
        {
            // excluded actions are not passed into VW
            // rankedIndices[0] is the index of the VW chosen action (1 based index)
            // ccb response that is converted into a cb response: the chosen action index is a field in the vw response.
            // multiSlotChosenActionIndex is part of the multi slot response (0 based index)
            int chosenActionIndex = multiSlotChosenActionIndex == -1 ? rankedIndices[0] - 1 : multiSlotChosenActionIndex;

            // take care of actions that are excluded in their original positions
            if (excludedActions != null && excludedActions.Count > 0)
            {
                var newRanking = new int[originalActions.Count];
                var probabilities = new float[originalActions.Count];

                // at the original position
                // point the original position of ranked item
                for (int i = 0; i < rankableActions.Count; i++)
                {
                    //RankableActions is Actions - ExcludedActions
                    newRanking[rankableActions[i].Index] = rankableActions[rankedIndices[i] - 1].Index + 1;
                    probabilities[rankableActions[i].Index] = rankingProbabilities[i];
                }

                // update excluded positions
                foreach (var l in excludedActions)
                    newRanking[l.Index] = l.Index + 1;

                rankedIndices = newRanking;
                rankingProbabilities = probabilities;
            }

            var personalizerRankResult = new PersonalizerRankResult
            {
                EventId = eventId
            };
            // finalize decision response ranking
            personalizerRankResult.Ranking = rankedIndices?.Select((index, i) =>
            {
                var action = originalActions[index - 1];
                return new PersonalizerRankedAction()
                {
                    Id = action.Id,
                    Probability = rankingProbabilities[i]
                };
            }).ToList();

            //setting RewardActionId to be the VW chosen action.
            personalizerRankResult.RewardActionId = originalActions.ElementAt(chosenActionIndex)?.Id;

            return personalizerRankResult;
        }
    }
}
