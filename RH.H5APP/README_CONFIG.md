# RH.H5App 配置快速参考

## ✅ 已添加的文件

### 1. **public/config.js** - 运行时配置
```javascript
window.__APP_CONFIG__ = {
  apiBaseUrl: 'https://192.168.1.113:8828',
  webBaseUrl: 'https://192.168.1.113:8848',
  httpsEnabled: true,
  // ... 更多配置
};
```
**特点**：生产环境可直接修改，无需重新构建

### 2. **public/web.config** - IIS 配置
- ✅ Vue Router HTML5 History 模式支持
- ✅ 静态资源缓存和压缩
- ✅ 可选的 IIS 反向代理规则

### 3. **src/types/config.d.ts** - TypeScript 类型
```typescript
declare global {
  interface Window {
    __APP_CONFIG__?: AppConfig;
  }
}
```
**特点**：完整的类型定义和 IDE 智能提示

### 4. **src/utils/request.ts** - 已适配
```typescript
const getServiceBaseUrl = () => {
  if (window.__APP_CONFIG__?.apiBaseUrl) {
    return window.__APP_CONFIG__.apiBaseUrl;
  }
  return ''; // 开发环境使用 Vite 代理
};
```

### 5. **index.html** - 已加载配置
```html
<!-- 在 main.ts 之前加载 -->
<script src="/config.js"></script>
<script type="module" src="/src/main.ts"></script>
```

## 📦 开发环境

```bash
# 安装依赖
npm install

# 启动开发服务器
npm run dev
```
访问：`http://localhost:5174`

API 请求通过 Vite 代理转发到后端。

## 🚀 生产环境部署

### 方式 1: IIS 反向代理（推荐）✅

1. **构建**
   ```bash
   npm run build
   ```

2. **修改配置**
   ```bash
   vim dist/config.js
   ```
   ```javascript
   window.__APP_CONFIG__ = {
     apiBaseUrl: 'https://www.example.com',  // 使用前端域名
     webBaseUrl: 'https://www.example.com/h5app',
     httpsEnabled: true
   };
   ```

3. **部署到 IIS**
   ```powershell
   # 创建目录
   New-Item -Path "C:\inetpub\wwwroot\h5app" -ItemType Directory -Force

   # 复制文件
   Copy-Item -Path "dist\*" -Destination "C:\inetpub\wwwroot\h5app" -Recurse

   # 在 IIS 中添加网站
   # 绑定: https://www.example.com (443)
   ```

4. **启用 API 代理**（可选）

   在 `dist/web.config` 中取消注释：
   ```xml
   <rule name="ReverseProxyApiRule" stopProcessing="true">
     <match url="^api/v1/(.*)" />
     <action type="Rewrite" url="http://localhost:8808/api/v1/{R|1}" />
   </rule>
   ```

### 方式 2: 独立域名

```javascript
// dist/config.js
window.__APP_CONFIG__ = {
  apiBaseUrl: 'https://api.example.com:8828',  // 独立 API 域名
  webBaseUrl: 'https://www.example.com/h5app'
};
```

## ⚙️ 配置说明

### 主要字段

| 字段 | 说明 | 开发环境 | 生产环境 |
|------|------|---------|---------|
| `apiBaseUrl` | API 地址 | 空（使用代理） | `https://www.example.com` |
| `webBaseUrl` | 前端地址 | `http://localhost:5174` | `https://www.example.com/h5app` |
| `httpsEnabled` | 启用 HTTPS | `false` | `true` |
| `apiTimeout` | 超时时间（毫秒） | `10000` | `15000` |
| `debugMode` | 调试模式 | `true` | `false` |

### 评价配置

```javascript
evaluationTypes: {
  options: [
    { label: '非常满意', value: 5 },
    { label: '满意', value: 4 },
    { label: '一般', value: 3 },
    { label: '不满意', value: 2 },
    { label: '非常不满意', value: 1 }
  ],
  requireContent: true,        // 是否必填
  contentMinLength: 10,        // 最小长度
  contentMaxLength: 500        // 最大长度
}
```

## 🧪 测试验证

### 1. 检查配置
```javascript
// 浏览器控制台
console.log(window.__APP_CONFIG__);
```

### 2. 检查 API 请求
```
浏览器控制台 → Network → XHR
查看请求 URL 是否正确
```

### 3. 移动端测试
```
Chrome DevTools → 设备工具栏 (Ctrl + Shift + M)
选择设备：iPhone 12 Pro
```

## 🔧 常见问题

### 修改 config.js 不生效
```bash
# 解决方法：清除缓存
1. 强制刷新：Ctrl + Shift + R
2. 清除浏览器缓存
3. 或 URL 后加版本号：?v=2
```

### API 请求 404
```javascript
// 检查 apiBaseUrl 是否正确
apiBaseUrl: 'https://www.example.com'  // 确保正确

// 如果使用 IIS 反向代理，确保取消注释 web.config 中的 API 代理规则
```

### Vue Router 刷新 404
```xml
<!-- 确保 web.config 包含 SPA 回退规则 -->
<rule name="SPAFallbackRule" stopProcessing="true">
  <match url=".*" />
  <conditions>
    <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
    <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
  </conditions>
  <action type="Rewrite" url="/" />
</rule>
```

## 📱 移动端优化

### 已配置的优化

1. **Viewport 设置**
   ```html
   <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=0" />
   ```

2. **静态资源缓存**
   ```xml
   <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
   ```

3. **GZIP 压缩**
   ```xml
   <urlCompression doStaticCompression="true" doDynamicCompression="true" />
   ```

4. **安全头**
   ```xml
   <add name="X-Content-Type-Options" value="nosniff" />
   <add name="X-Frame-Options" value="SAMEORIGIN" />
   <add name="X-XSS-Protection" value="1; mode=block" />
   ```

## 🎯 快速部署命令

```powershell
# 1. 构建
npm run build

# 2. 修改配置
notepad dist\config.js

# 3. 部署到 IIS
Copy-Item -Path "dist\*" -Destination "C:\inetpub\wwwroot\h5app" -Recurse -Force

# 4. 重启 IIS
iisreset
```

## 📄 相关文档

- [完整部署指南](./DEPLOY_H5APP.md)
- [IIS 反向代理部署](../RH.Admin/IIS_REVERSE_PROXY.md)
- [前端 HTTPS 部署](../RH.Admin/FRONTEND_HTTPS_FAQ.md)

---

## ✨ 特性总结

- ✅ 运行时配置（无需重新构建）
- ✅ IIS 部署支持
- ✅ Vue Router HTML5 History 模式
- ✅ TypeScript 类型安全
- ✅ 移动端优化
- ✅ HTTPS/HTTP 自动切换
- ✅ 生产环境示例配置

部署后可以直接修改 `dist/config.js` 来调整配置！
