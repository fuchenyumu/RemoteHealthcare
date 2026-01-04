# RH.H5App 部署指南

## 概述

RH.H5App 是远程会诊评价系统的 H5 移动端应用，支持运行时配置和 IIS 部署。

## 已添加的配置文件

### 1. `public/config.js` - 运行时配置文件
- ✅ 生产环境可随时修改，无需重新构建
- ✅ 包含 API 地址、证书配置、业务配置等
- ✅ 支持开发/生产环境自动切换

### 2. `public/web.config` - IIS 配置文件
- ✅ 支持 Vue Router HTML5 History 模式
- ✅ 静态资源缓存优化
- ✅ GZIP 压缩
- ✅ 可选的 IIS 反向代理规则
- ✅ 移动端优化配置

### 3. `src/types/config.d.ts` - TypeScript 类型定义
- ✅ 完整的配置类型定义
- ✅ 提供 `getAppConfig()` 等辅助函数
- ✅ 支持 IDE 智能提示

### 4. `src/utils/request.ts` - 已适配运行时配置
- ✅ 优先使用 `public/config.js` 配置
- ✅ 开发环境回退到 Vite 代理

### 5. `index.html` - 已加载配置文件
- ✅ 在 `main.ts` 之前加载 `config.js`

## 开发环境配置

### Vite 配置（`vite.config.ts`）

```typescript
export default defineConfig({
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:8828', // 后端 API 地址
        changeOrigin: true,
      },
    },
  },
})
```

### 运行开发环境

```bash
# 安装依赖
npm install

# 启动开发服务器
npm run dev
```

访问：`http://localhost:5174`

API 请求会通过 Vite 代理转发到 `http://localhost:8828`

## 生产环境部署

### 方式 1: IIS 反向代理（推荐）✅

#### 架构图

```
用户浏览器（移动端）
    ↓
https://www.example.com/h5app
    ↓
┌─────────────────────────────┐
│       IIS 服务器            │
│                             │
│  H5 App 站点                │
│  - 物理路径: C:\inetpub\wwwroot\h5app
│  - 绑定: HTTPS 443         │
│  - SSL 证书: Let's Encrypt │
│                             │
│  静态文件:                  │
│  - index.html              │
│  - config.js (运行时配置)   │
│  - assets/                 │
│                             │
│  URL 重写规则:              │
│  /api/* → http://localhost:8808/api/*
└─────────────────────────────┘
```

#### 配置步骤

1. **构建项目**
   ```bash
   npm run build
   ```
   生成 `dist/` 目录

2. **使用生产配置**
   ```bash
   # 复制生产配置
   cp public/config.production.js dist/config.js

   # 或编辑 config.js
   vim dist/config.js
   ```

3. **修改配置为实际域名**
   ```javascript
   window.__APP_CONFIG__ = {
     apiBaseUrl: 'https://www.example.com',  // 修改为实际域名
     webBaseUrl: 'https://www.example.com/h5app',
     httpsEnabled: true,
     ignoreCertificateErrors: false  // 生产环境使用有效证书
   };
   ```

4. **部署到 IIS**
   ```powershell
   # 创建网站目录
   New-Item -Path "C:\inetpub\wwwroot\h5app" -ItemType Directory -Force

   # 复制构建文件
   Copy-Item -Path "dist\*" -Destination "C:\inetpub\wwwroot\h5app" -Recurse -Force

   # 设置权限
   $acl = Get-Acl "C:\inetpub\wwwroot\h5app"
   $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
       "IUSR",
       "ReadAndExecute",
       "ContainerInherit,ObjectInherit",
       "None",
       "Allow"
   )
   $acl.SetAccessRule($accessRule)
   Set-Acl "C:\inetpub\wwwroot\h5app" $acl
   ```

5. **配置 IIS 站点**

   打开 IIS 管理器 → 添加网站：

   ```
   网站名称: RemoteHealthcare-H5App
   物理路径: C:\inetpub\wwwroot\h5app
   绑定类型: https
   端口: 443
   主机名: www.example.com
   SSL 证书: 选择证书
   应用程序池: DefaultAppPool 或新建
   ```

6. **启用 URL Rewrite（如果使用 API 代理）**

   在 `web.config` 中取消注释 API 代理规则：

   ```xml
   <rule name="ReverseProxyApiRule" stopProcessing="true">
     <match url="^api/v1/(.*)" />
     <action type="Rewrite" url="http://localhost:8808/api/v1/{R|1}" />
   </rule>
   ```

### 方式 2: 独立域名部署

#### 配置

```javascript
// dist/config.js
window.__APP_CONFIG__ = {
  apiBaseUrl: 'https://api.example.com:8828',  // 独立 API 域名
  webBaseUrl: 'https://www.example.com/h5app',
  httpsEnabled: true
};
```

#### 后端 CORS 配置

```json
// appsettings.json
{
  "App": {
    "CorsOrigins": "https://www.example.com,https://m.example.com"
  }
}
```

### 方式 3: 子目录部署

部署在主站点的子目录下（如 `/h5app/`）：

```javascript
// dist/config.js
window.__APP_CONFIG__ = {
  apiBaseUrl: 'https://www.example.com',
  webBaseUrl: 'https://www.example.com/h5app'
};
```

IIS 配置：
- 主站点指向 `C:\inetpub\wwwroot`
- H5 App 文件放在 `C:\inetpub\wwwroot\h5app\`

## 运行时配置修改

部署后可以直接修改配置文件，无需重新构建：

### 修改 API 地址

```bash
# 编辑配置文件
vim /path/to/wwwroot/config.js

# 修改 API 地址
apiBaseUrl: 'https://new-api.example.com'
```

### 修改业务配置

```javascript
evaluationTypes: {
  requireContent: false,  // 改为非必填
  contentMinLength: 5,    // 修改最小长度
  contentMaxLength: 1000  // 修改最大长度
}
```

### 刷新缓存

修改后需要清除浏览器缓存：
- 移动端：关闭浏览器重新打开
- 或在 URL 后添加时间戳：`?t=123456789`

## 配置文件说明

### `public/config.js` 字段说明

| 字段 | 说明 | 示例 |
|------|------|------|
| `apiBaseUrl` | API 基础地址 | `https://www.example.com` |
| `webBaseUrl` | Web 前端地址 | `https://www.example.com/h5app` |
| `httpsEnabled` | 是否启用 HTTPS | `true` |
| `apiTimeout` | API 超时时间（毫秒） | `10000` |
| `certificateFingerprint` | SSL 证书指纹 | `A1B2C3D4...` |
| `ignoreCertificateErrors` | 是否忽略证书错误 | `false`（生产环境） |
| `appTitle` | 应用标题 | `远程会诊评价` |
| `debugMode` | 调试模式 | `false` |

### `evaluationTypes` 配置说明

```javascript
evaluationTypes: {
  options: [
    { label: '非常满意', value: 5 },
    { label: '满意', value: 4 },
    // ...
  ],
  requireContent: true,      // 是否必填评价内容
  contentMinLength: 10,      // 评价内容最小长度
  contentMaxLength: 500      // 评价内容最大长度
}
```

## 测试验证

### 1. 检查配置加载

打开浏览器控制台：
```javascript
console.log(window.__APP_CONFIG__);
```

预期输出：
```json
{
  "apiBaseUrl": "https://www.example.com",
  "webBaseUrl": "https://www.example.com/h5app",
  "httpsEnabled": true,
  ...
}
```

### 2. 检查 API 请求

浏览器控制台 → Network 标签 → 筛选 XHR

查看请求 URL 是否正确：
```
✅ GET https://www.example.com/api/v1/evaluation/xxx
✅ POST https://www.example.com/api/v1/evaluation
```

### 3. 移动端测试

使用手机浏览器访问：
```
https://www.example.com/h5app
```

或使用 Chrome DevTools 设备模拟：
1. 打开开发者工具（F12）
2. 点击设备工具栏（Ctrl + Shift + M）
3. 选择设备（如 iPhone 12 Pro）

## 常见问题

### 1. 修改 config.js 后不生效

**原因**：浏览器缓存

**解决**：
- 强制刷新：Ctrl + Shift + R
- 清除浏览器缓存
- 或在 URL 后添加版本号：`/config.js?v=2`

### 2. API 请求 404

**原因**：
- API 地址配置错误
- 未启用 URL Rewrite 规则

**解决**：
```javascript
// 检查 config.js
apiBaseUrl: 'https://www.example.com'  // 确保正确
```

如果使用 IIS 反向代理，确保 `web.config` 中取消注释 API 代理规则。

### 3. CORS 错误

**问题**：
```
Access to XMLHttpRequest at 'https://api.example.com'
from origin 'https://www.example.com' has been blocked by CORS policy
```

**解决**：
1. 使用 IIS 反向代理（推荐）
2. 或在后端配置 CORS：
```json
// appsettings.json
{
  "App": {
    "CorsOrigins": "https://www.example.com"
  }
}
```

### 4. Vue Router 刷新 404

**解决**：确保 `web.config` 已配置 SPA 回退规则

```xml
<rule name="SPAFallbackRule" stopProcessing="true">
  <match url=".*" />
  <conditions>
    <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
    <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
  </conditions>
  <action type="Rewrite" url="/" />
</rule>
```

### 5. 移动端无法访问

**检查项**：
1. 域名是否可访问（DNS 解析）
2. SSL 证书是否有效（自签名证书需手动信任）
3. 防火墙是否开放 443 端口
4. IIS 站点是否绑定到正确的 IP 和端口

## 性能优化建议

### 1. 启用 GZIP 压缩

已在 `web.config` 中配置：
```xml
<urlCompression doStaticCompression="true" doDynamicCompression="true" />
```

### 2. 配置缓存策略

静态资源缓存 7 天：
```xml
<clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
```

### 3. 使用 CDN

将静态资源（图片、字体等）上传到 CDN：
```javascript
window.__APP_CONFIG__ = {
  cdnBaseUrl: 'https://cdn.example.com'
};
```

### 4. 代码分割

Vite 默认启用代码分割，生产构建会自动优化。

## 安全建议

### 1. 生产环境配置

```javascript
{
  httpsEnabled: true,
  ignoreCertificateErrors: false,  // 使用有效证书
  debugMode: false  // 关闭调试模式
}
```

### 2. 添加 CSP 头（可选）

在 `web.config` 中添加：
```xml
<httpProtocol>
  <customHeaders>
    <add name="Content-Security-Policy" value="default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';" />
  </customHeaders>
</httpProtocol>
```

### 3. 防止点击劫持

已在 `web.config` 中配置：
```xml
<add name="X-Frame-Options" value="SAMEORIGIN" />
```

## 参考文档

- [IIS URL Rewrite Module](https://docs.microsoft.com/en-us/iis/extensions/url-rewrite-module/)
- [Vite 部署指南](https://vitejs.dev/guide/build.html)
- [Vue Router History 模式](https://router.vuejs.org/guide/essentials/history-mode.html)

## 总结

RH.H5App 现在支持：
- ✅ 运行时配置（无需重新构建）
- ✅ IIS 部署（支持反向代理）
- ✅ HTTPS/HTTP 自动切换
- ✅ Vue Router HTML5 History 模式
- ✅ 移动端优化配置
- ✅ TypeScript 类型安全

部署后可以直接修改 `dist/config.js` 来调整配置，无需重新构建项目！
