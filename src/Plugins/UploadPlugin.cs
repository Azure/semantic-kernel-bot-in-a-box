using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.Linq;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using System.Collections.Generic;
using Microsoft.BotBuilderSamples;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Plugins;

public class UploadPlugin
{
    private readonly AzureTextEmbeddingGeneration _embeddingClient;
    private ConversationData _conversationData;
    private ITurnContext<IMessageActivity> _turnContext;

    public UploadPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, AzureTextEmbeddingGeneration embeddingClient)
    {
        _embeddingClient = embeddingClient;
        _conversationData = conversationData;
        _turnContext = turnContext;
    }


    [SKFunction, Description("Search for relevant information in the uploaded documents. Only use this when the user refers to documents they uploaded.")]
    public async Task<string> SearchUploads(
        [Description("The exact name of the document to be searched")] string docName,
        [Description("The text to search by similarity")] string query
    )
    {
        await _turnContext.SendActivityAsync($"Searching document {docName} for \"{query}\"...");
        var embedding = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { query });
        var vector = embedding.First().ToArray();
        var similarities = new List<float>();
        var attachment = _conversationData.Attachments.Find(x => x.Name == docName);
        foreach (AttachmentPage page in attachment.Pages)
        {
            float similarity = 0;
            for (int i = 0; i < page.Vector.Count(); i++)
            {
                similarity += page.Vector[i] * vector[i];
            }
            similarities.Add(similarity);
        }
        var maxIndex = similarities.IndexOf(similarities.Max());
        return _conversationData.Attachments.First().Pages[maxIndex].Content;
    }

}