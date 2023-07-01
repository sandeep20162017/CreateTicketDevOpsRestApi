using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;

class Program
{
	static HttpClient client = new HttpClient();

	static void Main(string[] args)
	{
		args.Append("bug");
		if (args.Length < 1)
		{
			Console.WriteLine("Invalid number of parameters. Usage: App.exe type [ParentBugId]");
			return;
		}

		string type = args[0];
		string parentBugId = args.Length > 1 ? args[1] : null;

		try
		{
			string configPath = "config.json";
			string jsonPath = type.ToLower() == "bug" ? "bug.json" : "task.json";

			JObject config = LoadJsonFromFile(configPath);
			string personalAccessToken = config["personalAccessToken"].ToString();
			string organizationUrl = config["organizationUrl"].ToString();

			CreateWorkItem(organizationUrl, personalAccessToken, jsonPath, type, parentBugId).Wait();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}
	}

	static async System.Threading.Tasks.Task CreateWorkItem(string organizationUrl, string personalAccessToken, string jsonPath, string type, string parentBugId)
	{
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
		

		JObject workItemJson = LoadJsonFromFile(jsonPath);
		string url = $"{organizationUrl}/_apis/wit/workitems/${type}?api-version=7.0";

		if (parentBugId != null && type.ToLower() == "bug")
		{
			JArray relations = workItemJson["relations"] as JArray;
			if (relations == null)
			{
				relations = new JArray();
				workItemJson["relations"] = relations;
			}

			JObject parentRelation = new JObject();
			parentRelation["rel"] = "System.LinkTypes.Hierarchy-Reverse";
			parentRelation["url"] = $"{organizationUrl}/TotalFundManagement/_apis/wit/workitems/{parentBugId}"; //check project name TotalFundManagement

			relations.Add(parentRelation);
		}

		string payload = workItemJson.ToString();
		var content = new StringContent(payload, Encoding.UTF8, "application/json-patch+json");

		
		using (HttpResponseMessage response = await client.PostAsync(url, content))
		{
			response.EnsureSuccessStatusCode();
			string responseBody = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"Work item created: {responseBody}");
		}
	}

	static JObject LoadJsonFromFile(string filePath)
	{
		string json = System.IO.File.ReadAllText(filePath);
		return JObject.Parse(json);
	}
}
