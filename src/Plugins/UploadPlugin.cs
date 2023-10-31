using System.ComponentModel;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.Linq;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using System.Collections.Generic;
using Microsoft.BotBuilderSamples;

namespace Plugins;

public class UploadPlugin
{
    private readonly AzureTextEmbeddingGeneration embeddingClient;
    private ConversationData _conversationData;

    public UploadPlugin(IConfiguration config, ConversationData conversationData)
    {
        var _aoaiApiKey = config.GetValue<string>("AOAI_API_KEY");
        var _aoaiApiEndpoint = config.GetValue<string>("AOAI_API_ENDPOINT");
        embeddingClient = new AzureTextEmbeddingGeneration(modelId: "text-embedding-ada-002", _aoaiApiEndpoint, _aoaiApiKey);
        _conversationData = conversationData;
    }



    [SKFunction, Description("Search for relevant information in the uploaded documents")]
    public async Task<string> SearchUploads(
        [Description("The text to search by similarity")] string query
    )
    {
        var embedding = await embeddingClient.GenerateEmbeddingsAsync(new List<string> { query });
        var vector = embedding.First().ToArray();
        var similarities = new List<float>();
        foreach (AttachmentPage page in _conversationData.Attachments.First().Pages)
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