using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WorkOrderManager
{
	class HttpClientLayer
	{
		private static HttpClientLayer INSTANCE = null;
		public static HttpClientLayer getInstance()
		{
			return INSTANCE ?? (INSTANCE = new HttpClientLayer());
		}

		private readonly HttpClient client;

		public HttpClientLayer()
		{
			client = new HttpClient();
		}

		public async Task<Message> Post(string url, Message message)
		{
			client.BaseAddress = new Uri(url);
			List<string> vSrc = new List<string> {message.source.ToString()};
			List<string> vDst = new List<string> {message.destination.ToString()};
			client.DefaultRequestHeaders.Add("source", vSrc);
			client.DefaultRequestHeaders.Add("destination", vDst);

			var response = await client.PostAsync(client.BaseAddress, new StringContent(message.ToString()));

			response.EnsureSuccessStatusCode();

			string content = await response.Content.ReadAsStringAsync();
			return await Task.Run(()  => Message.FromJson(content));
		}
	}
}
