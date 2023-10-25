// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Core;
using Azure.Identity;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
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
        private string _docIntelApiKey;
        private string _docIntelApiEndpoint;
        private StepwisePlanner _planner;
        private ILoggerFactory loggerFactory;
        private ILoggerProvider loggerProvider;
        private bool _debug;
        private IConfiguration _config;
        private DefaultAzureCredential credential;
        private readonly AzureTextEmbeddingGeneration embeddingClient;
        private readonly DocumentAnalysisClient documentAnalysisClient;

        public SKBot(IConfiguration config, ConversationState conversationState, UserState userState) : base(config, conversationState, userState)
        {
            _aoaiApiKey = config.GetValue<string>("AOAI_API_KEY");
            _aoaiApiEndpoint = config.GetValue<string>("AOAI_API_ENDPOINT");
            _aoaiModel = config.GetValue<string>("AOAI_MODEL");
            var defaultCredential = new DefaultAzureCredential();

            embeddingClient = _aoaiApiKey != null ?
                new AzureTextEmbeddingGeneration(modelId: "text-embedding-ada-002", _aoaiApiEndpoint, _aoaiApiKey) :
                new AzureTextEmbeddingGeneration(modelId: "text-embedding-ada-002", _aoaiApiEndpoint, defaultCredential);

            _docIntelApiKey = config.GetValue<string>("DOCINTEL_API_KEY");
            _docIntelApiEndpoint = config.GetValue<string>("DOCINTEL_API_ENDPOINT");

            documentAnalysisClient = new DocumentAnalysisClient(new Uri(_docIntelApiEndpoint), new AzureKeyCredential(_docIntelApiKey));

            _debug = config.GetValue<bool>("DEBUG");
            _config = config;
        }

        private IKernel GetKernel(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext) {
           
            loggerProvider = new ThoughtLoggerProvider(_debug, turnContext);
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
            kernel.ImportFunctions(new UploadPlugin(_config, conversationData), "UploadPlugin");
            kernel.ImportFunctions(new SQLPlugin(_config, conversationData), "SQLPlugin");
            kernel.ImportFunctions(new SearchPlugin(_config, conversationData), "SearchPlugin");
            return kernel;
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Welcome to GPTBot Sample. Type anything to get started.");
        }

        public override async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            // Handle file uploads
            if (turnContext.Activity.Attachments?.Count > 0)
            {
                return await HandleFileUpload(conversationData, turnContext);
            }

            kernel = GetKernel(conversationData, turnContext);

            var stepwiseConfig = new StepwisePlannerConfig
            {
                GetPromptTemplate = new StreamReader("./PromptConfig/StepwiseStepPrompt.json").ReadToEnd,
                MaxIterations = 15
            };
            _planner = new StepwisePlanner(kernel, stepwiseConfig);
            string prompt = FormatConversationHistory(conversationData);
            var plan = _planner.CreatePlan(prompt);

            var res = await kernel.RunAsync(plan);

            var stepsTaken = JsonConvert.DeserializeObject<Step[]>(res.FunctionResults.First().Metadata["stepsTaken"].ToString());

            return stepsTaken[stepsTaken.Length - 1].final_answer;
        }

        private async Task<string> HandleFileUpload(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            string endpoint = _docIntelApiEndpoint;
            string key = _docIntelApiKey;
            AzureKeyCredential credential = new AzureKeyCredential(key);
            DocumentAnalysisClient client = new DocumentAnalysisClient(new Uri(endpoint), credential);

            // Does not work locally - use the sample document on the next line to test
            Uri fileUri = new Uri(turnContext.Activity.Attachments.First().ContentUrl);
            // Uri fileUri = new Uri("https://www.sldttc.org/allpdf/21583473018.pdf");

            AnalyzeDocumentOperation operation = await documentAnalysisClient.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, "prebuilt-layout", fileUri);

            AnalyzeResult result = operation.Value;

            var attachment = new Attachment();
            foreach (DocumentPage page in result.Pages)
            {
                var attachmentPage = new AttachmentPage();
                attachmentPage.Content = "";
                for (int i = 0; i < page.Lines.Count; i++)
                {
                    DocumentLine line = page.Lines[i];
                    attachmentPage.Content += $"{line.Content}\n";
                }
                // Embed content
                var embedding = await embeddingClient.GenerateEmbeddingsAsync(new List<string> { attachmentPage.Content });
                attachmentPage.Vector = embedding.First().ToArray();
                attachment.Pages.Add(attachmentPage);
            }
            conversationData.Attachments.Add(attachment);

            return $"File {turnContext.Activity.Attachments[0].Name} uploaded successfully! {result.Pages.Count()} pages ingested.";
        }
    }
}
