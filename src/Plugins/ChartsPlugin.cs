using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using Microsoft.BotBuilderSamples;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Text.Json;

namespace Plugins;

public class Dataset {
    public string label { get; set; }
    public List<double> data { get; set; }
}

public class ChartData {
    public List<string> labels { get; set; }
    public List<Dataset> datasets { get; set; }
}

public class ChartOptions {
    public string type { get; set; }
    public ChartData data { get; set; }

}

public class ChartsPlugin
{
    private ConversationData _conversationData;
    private ITurnContext<IMessageActivity> _turnContext;

    public ChartsPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
    {
        _conversationData = conversationData;
        _turnContext = turnContext;
    }


    [SKFunction, Description("Render a chart with the given options")]
    public async Task<string> RenderChart(
        [Description("The type of chart. One of 'bar', 'line', or 'doughnut'")] string type,
        [Description("The main axis labels. Should be a serialized array of strings with the same size as each dataset's 'data' field.")] string labels,
        [Description("The datasets to be rendered. Should be a serialized JSON array where each element contains a 'label' (string) and a 'data' (list of decimal) field. Example: \"[{\"label\": \"Dogs\", \"data\": [ 50, 60, 70, 180, 190 ]}, {\"label\": \"Cats\", \"data\": [ 100, 200, 300, 400, 500 ]}]\" ")] string datasets
    )
    {
        try {
            ChartOptions opts = new()
            {
                type = type,
                data = new() {
                    labels = JsonSerializer.Deserialize<List<string>>(labels),
                    datasets = JsonSerializer.Deserialize<List<Dataset>>(datasets)
                }
            };
            List<object> images = new();
            images.Add(new { type = "Image", url = $"https://quickchart.io/chart?c={JsonSerializer.Serialize(opts)}" });
            object adaptiveCardJson = new
            {
                type = "AdaptiveCard",
                version = "1.0",
                body = images
            };
            var adaptiveCardAttachment = new Microsoft.Bot.Schema.Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = adaptiveCardJson,
            };
            await _turnContext.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment));
            return "Chart generated and sent to user.";
        } catch {
            return "Failed to generate a chart with the given inputs.";
        }
    }

}