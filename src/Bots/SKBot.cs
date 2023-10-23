// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;
using Model;
using Newtonsoft.Json;
using Plugins;

namespace Microsoft.BotBuilderSamples
{
    public class SKBot : StateManagementBot
    {
        private IKernel kernel;
        private string _aoaiApiKey;
        private string _aoaiApiEndpoint;
        private string _aoaiModel;
        private StepwisePlanner _planner;
        private ILoggerFactory loggerFactory;
        private ILoggerProvider loggerProvider;
        private IConfiguration _config;
        private DefaultAzureCredential credential;
        private TokenRequestContext tokenRequestContext;

        public SKBot(IConfiguration config, ConversationState conversationState, UserState userState) : base(config, conversationState, userState)
        {
            _aoaiApiKey = config.GetValue<string>("AOAI_API_KEY");
            _aoaiApiEndpoint = config.GetValue<string>("AOAI_API_ENDPOINT");
            _aoaiModel = config.GetValue<string>("AOAI_MODEL");
            _config = config;
            credential = new DefaultAzureCredential();
            tokenRequestContext = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Welcome to GPTBot Sample. Type anything to get started.");
        }

        public override async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {

            var uri = new Uri(_aoaiApiEndpoint);

            loggerProvider = new ThoughtLoggerProvider(_config, turnContext);
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddProvider(loggerProvider)
                    .AddConsole()
                    .AddDebug();
            });
            
            kernel = _aoaiApiKey != null ? 
                new KernelBuilder()
                    .WithAzureChatCompletionService(
                        deploymentName: _aoaiModel,
                        endpoint: _aoaiApiEndpoint,
                        apiKey: _aoaiApiKey
                    )
                    .WithLoggerFactory(loggerFactory)
                    .Build() : 
                new KernelBuilder()
                    .WithAzureChatCompletionService(
                        deploymentName: _aoaiModel,
                        endpoint: _aoaiApiEndpoint,
                        credentials: credential
                    )
                    .WithLoggerFactory(loggerFactory)
                    .Build();

            // kernel.ImportFunctions(new MathPlugin(), "MathPlugin");
            kernel.ImportFunctions(new SQLPlugin(_config), "SQLPlugin");
            kernel.ImportFunctions(new SearchPlugin(_config), "SearchPlugin");

            var stepwiseConfig = new StepwisePlannerConfig
            {
                // GetPromptTemplate = new StreamReader("./PromptConfig/StepwiseStepPrompt.txt").ReadToEnd,
                MaxIterations = 15
            };
            _planner = new StepwisePlanner(kernel, stepwiseConfig);
            string history = "";
            foreach (ConversationTurn conversationTurn in conversationData.History)
            {
                history += $"{conversationTurn.Role.ToUpper()}:\n{conversationTurn.Message}\n";
            }
            history += "ASSISTANT:";
            var plan = _planner.CreatePlan(history);

            Console.WriteLine(plan.ToJson(indented: true));
            var res = await kernel.RunAsync(plan);
            
            var stepsTaken = JsonConvert.DeserializeObject<Step[]>(res.FunctionResults.First().Metadata["stepsTaken"].ToString());
            
            return stepsTaken[stepsTaken.Length-1].final_answer;
        }

    }
}
