using BeerWallWeb.API.Entities;

namespace BeerMall.Api.Entities
{
    public class User
    {
        public long Id { get; set; }

        // 微信的核心标识
        public string OpenId { get; set; }

        public string NickName { get; set; } = "微信用户";
        public string AvatarUrl { get; set; } = "/static/default-avatar.png";
        public string? Mobile { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public long? InviterId { get; set; }

    }
    public class UserAddress
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public string Name { get; set; } // 联系人
        public string Mobile { get; set; } // 手机号
        public string Province { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Detail { get; set; } // 详细地址
        public bool IsDefault { get; set; } // 是否默认
    }
}