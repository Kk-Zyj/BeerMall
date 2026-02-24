using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace BeerWallWeb.Services
{
    public class WeChatService
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public WeChatService(IMemoryCache cache, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
         
        /// <summary>
        /// 获取微信 AccessToken (带自动缓存)
        /// </summary>
        public async Task<string> GetWeChatAccessTokenAsync()
        {
            string appId = _configuration["AppID"] ?? throw new InvalidOperationException("AppID 配置缺失");
            string secret = _configuration["AppSecret"] ?? throw new InvalidOperationException("AppSecret 配置缺失");

            // 1. 定义缓存 Key
            const string cacheKey = "WeChat_AccessToken";

            // 2. 尝试从缓存获取
            if (_cache.TryGetValue(cacheKey, out string accessToken))
            {
                return accessToken;
            }

            // 3. 缓存没有，则向微信服务器发起请求
            var client = _httpClientFactory.CreateClient();
            string url = $"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={appId}&secret={secret}";

            var response = await client.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();

            // 4. 解析响应
            var result = JsonConvert.DeserializeObject<WeChatTokenResponse>(jsonString);

            if (string.IsNullOrEmpty(result.AccessToken))
            {
                // 处理错误情况，记录日志
                throw new Exception($"获取 AccessToken 失败: {result.ErrMsg} (Code: {result.ErrCode})");
            }

            // 5. 写入缓存
            // 微信返回的 expires_in 是 7200 秒，我们为了安全起见，提前 5 分钟过期，设为 7200 - 300 秒
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(result.ExpiresIn - 300));

            _cache.Set(cacheKey, result.AccessToken, cacheEntryOptions);

            return result.AccessToken;
        }
    }

    // 定义响应实体类
    public class WeChatTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("errcode")]
        public int ErrCode { get; set; }

        [JsonProperty("errmsg")]
        public string ErrMsg { get; set; }
    }
}
