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

		public HttpClientLayer()
		{
		}

		public async Task<Message> Post(string url, Message message)
		{
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(url);
				
				client.Timeout = TimeSpan.FromMilliseconds(300);
				var response = await client.PostAsync(client.BaseAddress, new StringContent(message.ToString()));

				var content = await response.Content.ReadAsStringAsync();

				return await Task.Run(() => Message.FromJson(content));
			}
		}
	}
}
