# BeerMall 啤酒商城小程序（本地开发版）

一个本地可运行的啤酒商城：小程序前台（uni-app） + 后端 API（.NET + MySQL）。目前以学习/练手为主，功能在持续完善中，尚未部署到服务器。

## 项目结构

- `beer-mall-frontend/`：小程序前台（uni-app，Vue3 + Pinia + uview-plus）
- `BeerWallWeb/`：后端 API（ASP.NET Core + EF Core + MySQL + Swagger）

> 说明：前端默认调用 `https://localhost:7252`，与后端默认 https 端口保持一致。

---

## 技术栈

### 小程序前台（beer-mall-frontend）

- uni-app（Vue3）
- Pinia 状态管理
- uview-plus UI
- TypeScript（页面/Store 多处使用 `lang="ts"`）

页面包含：首页、分类、购物车、商品详情、订单（创建/详情/列表/裂变）、地址管理、我的等。

### 后端（BeerWallWeb）

- ASP.NET Core
- EF Core + Pomelo MySQL
- Swagger
- BackgroundService：订单超时关闭回滚库存、拼团过期处理
- 微信相关：code->openid 登录、手机号授权（access_token 缓存）

---

## 本地运行

### 1）准备 MySQL

创建数据库（默认名在配置里是 `beer_mall_db`）：

- MySQL 账号密码按你本机实际情况配置

### 2）启动后端 API（BeerWallWeb）

进入后端目录并运行：

```bash
cd BeerWallWeb/BeerWallWeb
dotnet restore
dotnet run
```
