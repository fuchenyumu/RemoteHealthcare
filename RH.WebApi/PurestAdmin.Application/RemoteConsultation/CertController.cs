// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace PurestAdmin.Application.RemoteConsultation
{
    /// <summary>
    /// 证书管理控制器 - 用于内网分享场景的证书下载和安装指引
    /// </summary>
    [ApiController]
    [Route("api/cert")]
    [AllowAnonymous] // 允许匿名访问，便于首次用户下载证书
    public class CertController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public CertController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// 下载 PEM 格式证书（兼容所有平台）
        /// </summary>
        [HttpGet("download")]
        public IActionResult DownloadCertificate()
        {
            var certPath = Path.Combine(_env.ContentRootPath, "dev-cert.pem");

            if (!System.IO.File.Exists(certPath))
            {
                return NotFound(new
                {
                    error = "证书文件不存在",
                    message = "请先运行 generate-prod-cert.bat 生成证书",
                    path = certPath
                });
            }

            return File(
                System.IO.File.ReadAllBytes(certPath),
                "application/x-x509-ca-cert",
                "dev-cert.pem"
            );
        }

        /// <summary>
        /// 下载 PFX 证书（Windows 更方便）
        /// </summary>
        [HttpGet("download/pfx")]
        public IActionResult DownloadPfx()
        {
            var pfxPath = Path.Combine(_env.ContentRootPath, "dev-cert.pfx");

            if (!System.IO.File.Exists(pfxPath))
            {
                return NotFound(new
                {
                    error = "PFX 证书不存在",
                    message = "请确认证书已生成并复制到项目目录"
                });
            }

            return File(
                System.IO.File.ReadAllBytes(pfxPath),
                "application/x-pkcs12",
                "dev-cert.pfx"
            );
        }

        /// <summary>
        /// 证书安装指引页面
        /// </summary>
        [HttpGet("install-guide")]
        public IActionResult InstallGuide([FromQuery] string returnUrl = "")
        {
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            var html = $@"
<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>🔒 证书安装指引 - 远程会诊系统</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Microsoft YaHei', sans-serif;
            line-height: 1.6;
            color: #333;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }}
        .container {{ 
            max-width: 800px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #0078d4 0%, #005a9e 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{ font-size: 28px; margin-bottom: 10px; }}
        .header p {{ font-size: 16px; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .warning {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .warning strong {{ color: #856404; }}
        .download-section {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            margin: 20px 0;
            text-align: center;
        }}
        .download-btn {{
            display: inline-block;
            background: #28a745;
            color: white;
            padding: 12px 24px;
            text-decoration: none;
            border-radius: 6px;
            margin: 10px 5px;
            font-weight: 500;
            transition: all 0.3s;
            box-shadow: 0 2px 8px rgba(40,167,69,0.3);
        }}
        .download-btn:hover {{
            background: #218838;
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(40,167,69,0.4);
        }}
        .device-tabs {{
            display: flex;
            gap: 10px;
            margin: 30px 0 20px 0;
            flex-wrap: wrap;
        }}
        .device-tabs button {{
            flex: 1;
            min-width: 120px;
            padding: 12px;
            border: 2px solid #e0e0e0;
            background: white;
            cursor: pointer;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 500;
            transition: all 0.3s;
        }}
        .device-tabs button:hover {{
            border-color: #0078d4;
            color: #0078d4;
        }}
        .device-tabs button.active {{
            background: #0078d4;
            color: white;
            border-color: #0078d4;
            box-shadow: 0 2px 8px rgba(0,120,212,0.3);
        }}
        .device-content {{
            display: none;
            animation: fadeIn 0.3s;
        }}
        .device-content.active {{ display: block; }}
        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(10px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
        .step {{
            background: #f8f9fa;
            padding: 20px;
            margin: 15px 0;
            border-left: 4px solid #0078d4;
            border-radius: 4px;
        }}
        .step h3 {{
            color: #0078d4;
            margin-bottom: 15px;
            font-size: 20px;
        }}
        .step ol {{
            margin-left: 20px;
        }}
        .step li {{
            margin: 10px 0;
            line-height: 1.8;
        }}
        .step ul {{
            margin: 10px 0 10px 20px;
        }}
        code {{
            background: #e83e8c;
            color: white;
            padding: 3px 8px;
            border-radius: 4px;
            font-size: 14px;
        }}
        .return-section {{
            background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%);
            padding: 20px;
            border-radius: 8px;
            margin: 20px 0;
            text-align: center;
        }}
        .return-btn {{
            background: white;
            color: #ff9800;
            padding: 12px 24px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: bold;
            display: inline-block;
            margin-top: 10px;
            transition: all 0.3s;
        }}
        .return-btn:hover {{
            transform: scale(1.05);
            box-shadow: 0 4px 12px rgba(0,0,0,0.2);
        }}
        .test-btn {{
            background: #17a2b8;
            color: white;
            padding: 12px 24px;
            text-decoration: none;
            border-radius: 6px;
            display: inline-block;
            margin: 10px 5px;
            transition: all 0.3s;
        }}
        .test-btn:hover {{
            background: #138496;
            transform: translateY(-2px);
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #666;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔒 HTTPS 证书安装指引</h1>
            <p>远程会诊系统 - 内网访问必须先安装信任证书</p>
        </div>

        <div class='content'>
            <div class='warning'>
                <strong>⚠️ 重要提示：</strong><br>
                本证书仅用于内网测试环境（192.168.1.113），请勿在公网使用。
                安装后如遇问题，可随时在系统设置中移除证书。
            </div>

            <div class='download-section'>
                <h2 style='margin-bottom: 15px;'>📥 第一步：下载证书</h2>
                <p style='margin-bottom: 15px; color: #666;'>根据你的设备选择合适的格式：</p>
                <a href='{baseUrl}/api/cert/download' class='download-btn'>
                    📄 下载 PEM 证书<br><small style='opacity: 0.8;'>(Android/iOS/macOS)</small>
                </a>
                <a href='{baseUrl}/api/cert/download/pfx' class='download-btn'>
                    🔑 下载 PFX 证书<br><small style='opacity: 0.8;'>(Windows 推荐)</small>
                </a>
            </div>

            <h2 style='margin: 30px 0 15px 0; text-align: center;'>📱 第二步：选择你的设备</h2>

            <div class='device-tabs'>
                <button onclick='showDevice(""windows"")' class='active' id='btn-windows'>
                    🪟 Windows
                </button>
                <button onclick='showDevice(""android"")' id='btn-android'>
                    🤖 Android
                </button>
                <button onclick='showDevice(""ios"")' id='btn-ios'>
                    🍎 iOS
                </button>
                <button onclick='showDevice(""mac"")' id='btn-mac'>
                    💻 macOS
                </button>
            </div>

            <div id='windows' class='device-content active'>
                <div class='step'>
                    <h3>🪟 Windows 安装步骤</h3>
                    <ol>
                        <li>点击上方 <code>下载 PFX 证书</code> 按钮</li>
                        <li>找到下载的 <code>dev-cert.pfx</code> 文件并<strong>双击</strong></li>
                        <li>证书导入向导会自动打开：
                            <ul>
                                <li>存储位置：选择 <strong>当前用户</strong></li>
                                <li>点击 <strong>下一步</strong></li>
                                <li>勾选 <strong>将所有证书放入下列存储</strong></li>
                                <li>点击 <strong>浏览</strong> → 选择 <strong>受信任的根证书颁发机构</strong></li>
                                <li>完成导入</li>
                            </ul>
                        </li>
                        <li><strong>关闭所有浏览器窗口</strong>并重新打开</li>
                        <li>点击下方<strong>测试连接</strong>验证安装</li>
                    </ol>
                    <p style='margin-top: 15px; color: #666;'>
                        <strong>💡 提示：</strong>如果导入后仍有警告，请清除浏览器缓存（Ctrl+Shift+Delete）后重试。
                    </p>
                </div>
            </div>

            <div id='android' class='device-content'>
                <div class='step'>
                    <h3>🤖 Android 安装步骤</h3>
                    <ol>
                        <li>点击上方 <code>下载 PEM 证书</code> 按钮</li>
                        <li>打开 <strong>设置</strong> 应用</li>
                        <li>进入 <strong>安全</strong> 或 <strong>安全与隐私</strong></li>
                        <li>找到 <strong>加密与凭据</strong> 或 <strong>更多安全设置</strong></li>
                        <li>选择 <strong>从存储设备安装</strong> 或 <strong>安装证书</strong></li>
                        <li>找到并选择下载的 <code>dev-cert.pem</code> 文件</li>
                        <li>如果系统要求，设置锁屏密码/PIN</li>
                        <li>为证书命名（如：远程会诊）并完成安装</li>
                        <li>打开<strong>系统浏览器</strong>（如 Chrome）访问分享链接</li>
                    </ol>
                    <p style='margin-top: 15px; color: #dc3545;'>
                        <strong>⚠️ 注意：</strong>
                        <ul>
                            <li>部分国产浏览器可能不支持自定义证书，请使用系统自带浏览器</li>
                            <li>Android 11+ 可能需要在应用设置中允许使用用户证书</li>
                        </ul>
                    </p>
                </div>
            </div>

            <div id='ios' class='device-content'>
                <div class='step'>
                    <h3>🍎 iOS 安装步骤</h3>
                    <ol>
                        <li>使用 <strong>Safari 浏览器</strong> 打开本页面</li>
                        <li>点击上方 <code>下载 PEM 证书</code> 按钮</li>
                        <li>Safari 会提示 <strong>此网站正尝试下载配置描述文件</strong>，点击 <strong>允许</strong></li>
                        <li>打开 <strong>设置</strong> App</li>
                        <li>进入 <strong>通用</strong> → <strong>VPN与设备管理</strong></li>
                        <li>找到 <strong>已下载的描述文件</strong>，点击安装</li>
                        <li>输入密码确认安装</li>
                        <li><strong>⭐ 关键步骤：</strong> 返回 <strong>设置</strong> → <strong>通用</strong> → <strong>关于本机</strong></li>
                        <li>滑到底部，点击 <strong>证书信任设置</strong></li>
                        <li>找到刚才安装的证书并<strong>启用完全信任</strong></li>
                        <li>重新打开 Safari 访问分享链接</li>
                    </ol>
                    <p style='margin-top: 15px; color: #dc3545;'>
                        <strong>⚠️ 重要：</strong>iOS 对自签名证书非常严格，第 8-10 步的<strong>证书信任设置</strong><strong>必须完成</strong>，否则无法访问！
                    </p>
                </div>
            </div>

            <div id='mac' class='device-content'>
                <div class='step'>
                    <h3>💻 macOS 安装步骤</h3>
                    <ol>
                        <li>点击上方 <code>下载 PEM 证书</code> 按钮</li>
                        <li>找到下载的 <code>dev-cert.pem</code> 文件并<strong>双击</strong></li>
                        <li><strong>钥匙串访问</strong> 应用会自动打开</li>
                        <li>在证书列表中找到刚导入的证书</li>
                        <li>双击证书打开详情窗口</li>
                        <li>展开 <strong>信任</strong> 部分</li>
                        <li>在 <strong>使用此证书时</strong> 下拉菜单中选择 <strong>始终信任</strong></li>
                        <li>关闭窗口，系统会要求输入密码确认</li>
                        <li>重启浏览器访问分享链接</li>
                    </ol>
                </div>
            </div>

            <div class='step' style='text-align: center;'>
                <h3>🧪 第三步：验证安装</h3>
                <p style='margin: 15px 0;'>安装完成后，点击下方按钮测试证书是否生效：</p>
                <a href='{baseUrl}/' target='_blank' class='test-btn'>
                    🔍 测试 HTTPS 连接
                </a>
                <p style='margin-top: 15px; color: #666; font-size: 14px;'>
                    如果能正常打开页面且<strong>没有证书警告</strong>，说明安装成功！
                </p>
            </div>

            {(!string.IsNullOrEmpty(returnUrl) ? $@"
            <div class='return-section'>
                <h3 style='color: white; margin-bottom: 10px;'>✅ 证书安装完成！</h3>
                <p style='color: white; margin-bottom: 15px;'>现在可以访问分享内容了</p>
                <a href='{returnUrl}' class='return-btn'>
                    🎯 返回分享链接
                </a>
            </div>
            " : "")}

            <div class='step'>
                <h3>❓ 常见问题</h3>
                <ul style='line-height: 2;'>
                    <li><strong>安装后仍显示不安全？</strong> → 完全关闭浏览器（包括后台进程）后重新打开</li>
                    <li><strong>找不到证书文件？</strong> → 检查浏览器下载记录，通常在 <code>下载</code> 文件夹</li>
                    <li><strong>iOS 安装后无法访问？</strong> → 务必完成<strong>证书信任设置</strong>中的启用步骤</li>
                    <li><strong>微信内无法访问？</strong> → 微信不支持自签名证书，请复制链接到系统浏览器打开</li>
                    <li><strong>如何删除证书？</strong> → 
                        <ul>
                            <li>Windows: 运行 <code>certmgr.msc</code> → 受信任的根证书 → 找到并删除</li>
                            <li>Android: 设置 → 安全 → 凭据存储 → 用户证书 → 删除</li>
                            <li>iOS: 设置 → 通用 → VPN与设备管理 → 删除描述文件</li>
                            <li>macOS: 钥匙串访问 → 找到证书 → 右键删除</li>
                        </ul>
                    </li>
                </ul>
            </div>
        </div>

        <div class='footer'>
            <p>🏥 远程会诊系统 | 技术支持</p>
            <p style='margin-top: 5px; font-size: 12px;'>证书有效期：1年 | 仅限内网使用</p>
        </div>
    </div>

    <script>
        function showDevice(device) {{
            // 隐藏所有内容
            document.querySelectorAll('.device-content').forEach(el => el.classList.remove('active'));
            document.querySelectorAll('.device-tabs button').forEach(el => el.classList.remove('active'));
            
            // 显示选中内容
            document.getElementById(device).classList.add('active');
            document.getElementById('btn-' + device).classList.add('active');
        }}
    </script>
</body>
</html>";

            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// 检查证书状态（前端判断用）
        /// </summary>
        [HttpGet("check")]
        public IActionResult CheckCertificate()
        {
            return Ok(new
            {
                status = "ok",
                message = "如果能看到此内容，说明证书已信任",
                server = Request.Host.ToString(),
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 获取服务器信息（用于前端显示）
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var certPath = Path.Combine(_env.ContentRootPath, "dev-cert.pem");
            var pfxPath = Path.Combine(_env.ContentRootPath, "dev-cert.pfx");

            return Ok(new
            {
                server = Request.Host.ToString(),
                certAvailable = System.IO.File.Exists(certPath),
                pfxAvailable = System.IO.File.Exists(pfxPath),
                installGuideUrl = $"{Request.Scheme}://{Request.Host}/api/cert/install-guide"
            });
        }
    }
}

