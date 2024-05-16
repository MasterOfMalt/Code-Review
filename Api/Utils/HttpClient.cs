namespace Api.Utils
{
    public class HttpApi
    {
        /// <summary>
        /// Singleton instance of HttpApi to avoid socket exhaustion
        /// https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        /// </summary>
        public static HttpApi Instance { get; } = new HttpApi();

        private readonly HttpClient Client;

        public HttpApi(HttpClient client = null)
        {
            Client = client ?? new HttpClient();
        }

        public Task<HttpResponseMessage> Send(HttpRequestMessage msg)
        {
            return Task.Run(() => Client.SendAsync(msg));
        }
    }
}
