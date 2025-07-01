using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Sinequa.Common;
using ModelContextProtocol.Server;
using Sinequa.Search.JsonMethods;


namespace Sinequa.Plugins;

public class ContextSimplifiedItem
{
    [JsonPropertyName("title")] public required string Title { get; set; }

    [JsonPropertyName("url")] public required string Url { get; set; }

    [JsonPropertyName("authors")] public required string Authors { get; set; }

    [JsonPropertyName("modified")] public required string Modified { get; set; }

    [JsonPropertyName("context")] public required string Context { get; set; }
}

public class EnterpriseSearchMcpPlugin : McpServerPlugin
{		

	private const string APP = "kiwAI";
    private const string QUERY = "_query";

	[McpServerToolType]
	public class EnterpriseSearchTools(IHttpContextAccessor contextAccessor)
	{
		[McpServerTool, Description("Returns text chunks of the company documents, that are relevant to the supplied search query")]
		public ContextSimplifiedItem[] EnterpriseSearch(string searchQuery)
		{
			// 1. Retireve the search session from the request context 
			var httpContext = contextAccessor.HttpContext ?? throw new Exception("HttpContext unavailable!");
			var session = httpContext.GetLoggedInSearchSession();

			// 2. Build the json method in charge of executing the assistant simplified context plugin
			var jsonMethod = JsonMethod.NewMethod(JsonMethodType.Plugin, session);
			
			var jsonMethodPayload = Json.NewObject();
			jsonMethodPayload.Set("Plugin", "ContextSimplified");

			jsonMethod.JsonRequest = jsonMethodPayload;

			// 3. Create the json method plugin wrapped by the main json metod defined above
			if (!jsonMethod.CreatePlugin())
				throw new Exception("Failed to create plugin 'ContextSimplified' make sure that the latest version of the assistant is configured.");

			// app and query are harcoded in this plugin and should be adpated
			jsonMethod.Plugin.Method.Context = $"{APP}/{QUERY}";
			var pluginPayload = Json.NewObject();
			pluginPayload.Set("search_query", searchQuery);

			jsonMethod.Plugin.Method.JsonRequest = pluginPayload;

			// 4. Execute the simplified context json method and map the output to ContextSimplifiedItem instance			
			jsonMethod.Plugin.Method.Execute();

			return ((JsonArray) jsonMethod.JsonResponse)
				.EnumerateElements()
				.Select(item =>
					new ContextSimplifiedItem
					{
						Title = item.GetValue("title") ?? string.Empty,
						Url = item.GetValue("url") ?? string.Empty,
						Authors = item.GetValue("authors") ?? string.Empty,
						Modified = item.GetValue("modified") ?? string.Empty,
						Context = item.GetValue("context") ?? string.Empty
					}).ToArray();
		}
	}

	public override IEnumerable<Type> GetToolTypes() => [typeof(EnterpriseSearchTools)];
}
