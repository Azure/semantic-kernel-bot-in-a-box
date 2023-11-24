// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Planners;
using Model;
using Newtonsoft.Json;
using Plugins;

namespace Microsoft.BotBuilderSamples
{
    public class SemanticKernelBot : StateManagementBot
    {
        private IKernel kernel;
        private string _aoaiModel;
        private StepwisePlanner _planner;
        private ILoggerFactory loggerFactory;
        private IConfiguration _config;
        private readonly OpenAIClient _aoaiClient;
        private readonly SearchClient _searchClient;
        private readonly AzureTextEmbeddingGeneration _embeddingsClient;
        private readonly DocumentAnalysisClient _documentAnalysisClient;
        private readonly SqlConnectionFactory _sqlConnectionFactory;

        public SemanticKernelBot(IConfiguration config, ConversationState conversationState, UserState userState, OpenAIClient aoaiClient, AzureTextEmbeddingGeneration embeddingsClient, DocumentAnalysisClient documentAnalysisClient = null, SearchClient searchClient = null, SqlConnectionFactory sqlConnectionFactory = null) : base(config, conversationState, userState)
        {
            _aoaiModel = config.GetValue<string>("AOAI_GPT_MODEL");

            _aoaiClient = aoaiClient;
            _searchClient = searchClient;
            _embeddingsClient = embeddingsClient;
            _documentAnalysisClient = documentAnalysisClient;
            _sqlConnectionFactory = sqlConnectionFactory;

            _config = config;
        }

        private IKernel GetKernel(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext) {
           
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole()
                    .AddDebug();
            });
            
            kernel = new KernelBuilder()
                    .WithAzureChatCompletionService(
                        deploymentName: _aoaiModel,
                        _aoaiClient
                    )
                    .WithLoggerFactory(loggerFactory)
                    .Build();

            if (_sqlConnectionFactory != null) kernel.ImportFunctions(new SQLPlugin(conversationData, turnContext, _sqlConnectionFactory), "SQLPlugin");
            if (_documentAnalysisClient != null) kernel.ImportFunctions(new UploadPlugin(conversationData, turnContext, _embeddingsClient), "UploadPlugin");
            if (_searchClient != null) kernel.ImportFunctions(new HotelsPlugin(conversationData, turnContext, _searchClient), "HotelsPlugin");
            kernel.ImportFunctions(new DALLEPlugin(conversationData, turnContext, _aoaiClient), "DALLEPlugin");
            kernel.ImportFunctions(new ChartsPlugin(conversationData, turnContext), "ChartsPlugin");
            return kernel;
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Welcome to GPTBot Sample. Type anything to get started.");
        }

        public override async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            // If there are PDF files attached, ingest them
            if (
                turnContext.Activity.Attachments?.Count > 0 && 
                turnContext.Activity.Attachments.Any(x => x.ContentType == "application/pdf")
            )
            {
                if (!_config.GetValue<string>("DOCINTEL_API_ENDPOINT").IsNullOrEmpty()) {
                    var textresponse = "";
                    foreach (Bot.Schema.Attachment pdfAttachment in turnContext.Activity.Attachments.Where(x => x.ContentType == "application/pdf")) {
                        textresponse += await HandleFileUpload(conversationData, pdfAttachment) + "\n";
                    }
                    return textresponse;
                }
                else
                    return "Document upload not supported as no Document Intelligence endpoint was provided";
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

        private async Task<string> HandleFileUpload(ConversationData conversationData, Bot.Schema.Attachment pdfAttachment)
        {
            Uri fileUri = new Uri(pdfAttachment.ContentUrl);

            var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(fileUri);

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            var operation = await _documentAnalysisClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", ms);
            
            ms.Dispose();

            AnalyzeResult result = operation.Value;

            var attachment = new Attachment();
            attachment.Name = pdfAttachment.Name;
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
                var embedding = await _embeddingsClient.GenerateEmbeddingsAsync(new List<string> { attachmentPage.Content });
                attachmentPage.Vector = embedding.First().ToArray();
                attachment.Pages.Add(attachmentPage);
            }
            conversationData.Attachments.Add(attachment);

            return $"File {pdfAttachment.Name} uploaded successfully! {result.Pages.Count()} pages ingested.";
        }
    }
}
