namespace BeerWallWeb.Services
{
    public class WeChatNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public WeChatNotificationService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // 发送任务成功通知
        public async Task SendTaskSuccessAsync(string openId, string taskName, string orderNo)
        {
            var data = new
            {
                thing1 = new { value = taskName }, // 任务名称：免单挑战
                character_string2 = new { value = orderNo }, // 订单号
                thing3 = new { value = "任务已达标，请点击联系店主领奖！" } // 温馨提示
            };
            await SendTemplateMessage(openId, "你的成功模板ID", data);
        }

        // 发送任务回退通知
        public async Task SendTaskRegressAsync(string openId, string taskName, int currentCount, int targetCount)
        {
            var data = new
            {
                thing1 = new { value = taskName }, // 任务名称
                thing2 = new { value = "有好友退单，任务进度回退" }, // 变动原因
                thing3 = new { value = $"当前进度 {currentCount}/{targetCount}，请继续邀请" } // 当前进度
            };
            await SendTemplateMessage(openId, "你的变动模板ID", data);
        }

        private async Task SendTemplateMessage(string openId, string templateId, object data)
        {
            // 1. 获取 AccessToken (实际项目中应该缓存 Token，这里简化)
            string appId = _config["WeChat:AppId"];
            string secret = _config["WeChat:AppSecret"];
            // ... 获取Token逻辑 ...
            string token = "MOCK_TOKEN";

            // 2. 发送请求
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                touser = openId,
                template_id = templateId,
                page = "pages/order/detail", // 点击卡片跳转的页面
                data = data
            };

            await client.PostAsJsonAsync($"https://api.weixin.qq.com/cgi-bin/message/subscribe/send?access_token={token}", payload);
        }
    }
}
