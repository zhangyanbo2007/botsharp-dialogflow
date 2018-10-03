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
using BotSharp.Platform.Models.AiRequest;

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
            AgentModel agent = null;
            string projectsPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects");

            string[] d1 = Directory.GetDirectories(projectsPath);
            for (int i = 0; i < d1.Length; i++)
            {
                string[] d2 = Directory.GetDirectories(d1[i]);
                var raw = Path.Combine(d1[i], "tmp");
                if (!Directory.Exists(raw))
                {
                    continue;
                }

                string metaJson = System.IO.File.ReadAllText(Path.Combine(raw, "meta.json"));

                var meta = JsonConvert.DeserializeObject<AgentImportHeader>(metaJson);

                if (meta.ClientAccessToken == clientAccessToken)
                {
                    agent = builder.GetAgentById(meta.Id);
                    break;
                }
            };

            if(agent == null)
            {
                return BadRequest("The agent not found.");
            }

            var aIResponse = builder.TextRequest(new AiRequest
            {
                Text = request.Query,
                AgentId = agent.Id,
                SessionId = request.SessionId
            });

            return null;
        }
    }
}
