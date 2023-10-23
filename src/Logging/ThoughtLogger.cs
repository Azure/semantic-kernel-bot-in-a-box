
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class ThoughtLoggerProvider : ILoggerProvider
{
    private ITurnContext<IMessageActivity> _turnContext;
    private IConfiguration _config;
    public ThoughtLoggerProvider(IConfiguration config, ITurnContext<IMessageActivity> turnContext)
    {
        _turnContext = turnContext;
        _config = config;
    }
    public ILogger CreateLogger(string categoryName)
    {
        return new ThoughtLogger(_config, _turnContext);
    }

    public void Dispose() { }

}

public class ThoughtLogger : ILogger
{
    private ITurnContext<IMessageActivity> _turnContext;
    private bool _debug;
    public ThoughtLogger(IConfiguration config, ITurnContext<IMessageActivity> turnContext)
    {
        _turnContext = turnContext;
        _debug = config.GetValue<bool>("DEBUG");
    }
    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (_debug)
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, dynamic>[]>(System.Text.Json.JsonSerializer.Serialize(state));
            var action = json.Where(item => item.GetValueOrDefault("Key", null) == "Action").FirstOrDefault()?["Value"];
            var thought = json.Where(item => item.GetValueOrDefault("Key", null) == "Thought").FirstOrDefault()?["Value"];
            var observation = json.Where(item => item.GetValueOrDefault("Key", null) == "Observation").FirstOrDefault()?["Value"];
            if (action != null && action != "GetChatCompletionsAsync")
                await _turnContext.SendActivityAsync($"Action: {action}\n\nInput: {json[1].GetValueOrDefault("Value")}");
            if (observation != null)
                await _turnContext.SendActivityAsync($"Observation: {observation}");
            if (thought != null)
                await _turnContext.SendActivityAsync($"Thought: {thought}");
        }
    }
}