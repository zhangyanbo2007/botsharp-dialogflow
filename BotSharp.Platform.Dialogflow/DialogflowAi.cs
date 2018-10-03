using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core;
using BotSharp.Core.Engines;
using BotSharp.NLP;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using DotNetToolkit;
using BotSharp.Platform.Dialogflow.Models;
using System.IO;
using BotSharp.Platform.Models.MachineLearning;
using BotSharp.Models.NLP;

namespace BotSharp.Platform.Dialogflow
{
    public class DialogflowAi<TAgent> :
        PlatformBuilderBase<TAgent>,
        IPlatformBuilder<TAgent>
        where TAgent : AgentModel
    {
        public TrainingCorpus ExtractorCorpus(TAgent agent)
        {
            var corpus = new TrainingCorpus
            {
                Entities = new List<TrainingEntity>(),
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>()
            };

            agent.Entities.ForEach(entity =>
            {
                corpus.Entities.Add(new TrainingEntity
                {
                    Entity = entity.Name,
                    Values = entity.Entries.Select(x => new TrainingEntitySynonym
                    {
                        Value = x.Value,
                        Synonyms = x.Synonyms.Select(y => y.Synonym).ToList()
                    }).ToList()
                });
            });

            agent.Intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(say => {
                    corpus.UserSays.Add(new TrainingIntentExpression<TrainingIntentExpressionPart>
                    {
                        Intent = intent.Name,
                        Text = String.Join("", say.Data.Select(x => x.Text)),
                        Entities = say.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                        .Select(x => new TrainingIntentExpressionPart
                        {
                            Value = x.Text,
                            Entity = x.Meta,
                            Start = x.Start
                        })
                        .ToList()
                    });
                });
            });

            return corpus;
        }

        public async Task<ModelMetaData> Train(TAgent agent, TrainingCorpus corpus)
        {
            string agentDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", agent.Id);
            var model = "model_" + DateTime.UtcNow.ToString("yyyyMMdd");

            var trainer = new BotTrainer();
            agent.Corpus = corpus;

            var trainOptions = new BotTrainOptions
            {
                AgentDir = agentDir,
                Model = model
            };

            var info = await trainer.Train(agent, trainOptions);

            return info;
        }

        public AiResponse TextRequest(AiRequest request)
        {
            var aiResponse = new AiResponse();

            // Load agent
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", request.AgentId);
            var model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            var modelPath = Path.Combine(projectPath, model);
            request.AgentDir = projectPath;
            request.Model = model;

            var agent = GetAgentById(request.AgentId);

            var preditor = new BotPredictor();
            var doc = preditor.Predict(agent, request).Result;

            var parameters = new Dictionary<String, Object>();
            if (doc.Sentences[0].Entities == null)
            {
                doc.Sentences[0].Entities = new List<NlpEntity>();
            }
            doc.Sentences[0].Entities.ForEach(x => parameters[x.Entity] = x.Value);

            aiResponse.Intent = doc.Sentences[0].Intent.Label;
            aiResponse.Speech = aiResponse.Intent;

            return aiResponse;
        }
    }
}
