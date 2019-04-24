using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WorkOrderManager
{
	internal class HttpClientLayer
	{
		private static HttpClientLayer INSTANCE;

		public static HttpClientLayer getInstance()
		{
			return INSTANCE ?? (INSTANCE = new HttpClientLayer());
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