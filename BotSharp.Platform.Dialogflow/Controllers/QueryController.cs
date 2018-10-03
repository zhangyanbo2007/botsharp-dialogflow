using BotSharp.Core.Engines;
using BotSharp.NLP;
using BotSharp.Platform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using BotSharp.Platform.Dialogflow.Models;
using BotSharp.Platform.Dialogflow.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using BotSharp.Platform.Abstraction;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Platform.Dialogflow.Controllers
{
    /// <summary>
    /// Dialogflow mode query controller
    /// </summary>
    [Authorize]
    [Route("v1/[controller]")]
    public class QueryController : ControllerBase
    {
        private DialogflowAi<AgentModel> builder;

        public QueryController(IConfiguration configuration)
        {
            builder = new DialogflowAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("DialogflowAi");
        }

        /// <summary>
        /// The query endpoint is used to process natural language in the form of text. 
        /// The query requests return structured data in JSON format with an action and parameters for that action.
        /// Both GET and POST methods return the same JSON response.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet, HttpPost]
        public ActionResult<AIResponse> Query(QueryModel request)
        {
            String clientAccessToken = (User.Identity as ClaimsIdentity).Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

            // find a model according to clientAccessToken
            string projectPath = String.Empty;
            string projectsPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects");

            string[] d1 = Directory.GetDirectories(projectsPath);
            for (int i = 0; i < d1.Length; i++)
            {
                string[] d2 = Directory.GetDirectories(d1[i]);
                var raw = Path.Combine(d1[i], "tmp");
                if (Directory.Exists(raw))
                {

                }

                for (int j = 0; j < d2.Length; j++)
                {
                    string metaJson = System.IO.File.ReadAllText(Path.Combine(d2[j], "meta.json"));

                    var meta = JsonConvert.DeserializeObject<AgentImportHeader>(metaJson);

                    if (meta.ClientAccessToken == clientAccessToken)
                    {
                        projectPath = d1[i];
                        break;
                    }
                };

                if (!String.IsNullOrEmpty(projectPath))
                {
                    break;
                }
            };

            // Load agent
            string model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            string dataDir = Path.Combine(projectPath, model);
            
            if(!System.IO.File.Exists(Path.Combine(dataDir, "model-meta.json")))
            {
                return BadRequest("The agent hasn't been trained. Please train the agent before querying.");
            }

            Console.WriteLine($"LoadAgentFromFile {dataDir}");

            //_platform.LoadAgentFromFile(dataDir);

            /*var aIResponse = _platform.TextRequest(new AIRequest
            {
                Timezone = request.Timezone,
                Contexts = request?.Contexts?.Select(x => new AIContext { Name = x })?.ToList(),
                Language = request.Lang,
                Query = new String[] { request.Query },
                AgentDir = projectPath,
                Model = model
            });*/

            return null;
        }
    }
}
