/**
 * 证书检测工具
 * 用于检查用户设备是否已安装并信任 HTTPS 证书
 */

import { ElMessage, ElMessageBox } from "element-plus";

/**
 * 检查证书是否已安装
 */
export async function checkCertificate(): Promise<boolean> {
  const apiBaseUrl = import.meta.env.VITE_BASE_URL || "";

  try {
    const response = await fetch(`${apiBaseUrl}/api/cert/check`, {
      mode: "cors",
      cache: "no-cache",
      timeout: 5000
    } as RequestInit);

    if (response.ok) {
      const data = await response.json();
      console.log("✅ 证书检测成功:", data);
      return true;
    }

    return false;
  } catch (error: any) {
    console.error("❌ 证书检测失败:", error);

    // 检测错误类型
    if (
      error.message?.includes("certificate") ||
      error.message?.includes("SSL") ||
      error.message?.includes("CERT") ||
      error.name === "TypeError"
    ) {
      return false; // 证书问题
    }

    // 其他网络错误
    return false;
  }
}

/**
 * 检查证书并自动引导用户安装
 * @param showMessage 是否显示消息提示
 * @param autoRedirect 检测到未安装时是否自动跳转
 * @returns 证书是否已安装
 */
export async function checkAndGuide(
  showMessage = true,
  autoRedirect = false
): Promise<boolean> {
  const installed = await checkCertificate();

  if (!installed) {
    const apiBaseUrl = import.meta.env.VITE_BASE_URL || "";
    const currentUrl = window.location.href;
    const installUrl = `${apiBaseUrl}/api/cert/install-guide?returnUrl=${encodeURIComponent(currentUrl)}`;

    if (autoRedirect) {
      if (showMessage) {
        ElMessage.warning({
          message: "检测到未安装证书，即将跳转到安装指引页面...",
          duration: 2000
        });
      }

      setTimeout(() => {
        window.location.href = installUrl;
      }, 2000);
    } else if (showMessage) {
      try {
        await ElMessageBox.confirm(
          "检测到您的设备未安装信任证书，无法正常访问分享内容。是否前往安装指引页面？",
          "⚠️ 证书未安装",
          {
            confirmButtonText: "前往安装",
            cancelButtonText: "稍后再说",
            type: "warning",
            center: true
          }
        );

        window.location.href = installUrl;
      } catch {
        // 用户取消
      }
    }

    return false;
  }

  if (showMessage) {
    ElMessage.success("✅ 证书已安装，可以正常访问");
  }

  return true;
}

/**
 * 获取证书安装指引 URL
 */
export function getInstallGuideUrl(returnUrl?: string): string {
  const apiBaseUrl = import.meta.env.VITE_BASE_URL || "";
  const url = `${apiBaseUrl}/api/cert/install-guide`;

  if (returnUrl) {
    return `${url}?returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  return url;
}

/**
 * 复制安装指引到剪贴板
 */
export async function copyInstallGuide(shareUrl: string): Promise<boolean> {
  const installUrl = getInstallGuideUrl(shareUrl);

  const text = `🔗 分享链接：
${shareUrl}

📱 证书安装指引（首次访问必看）：
${installUrl}

⚠️ 重要：请先访问证书安装指引，按照步骤安装证书后，再打开分享链接。`;

  try {
    await navigator.clipboard.writeText(text);
    ElMessage.success("完整信息已复制到剪贴板");
    return true;
  } catch (error) {
    console.error("复制失败:", error);
    ElMessage.error("复制失败，请手动复制");
    return false;
  }
}

/**
 * 获取证书信息
 */
export async function getCertInfo(): Promise<any> {
  const apiBaseUrl = import.meta.env.VITE_BASE_URL || "";

  try {
    const response = await fetch(`${apiBaseUrl}/api/cert/info`);
    return await response.json();
  } catch (error) {
    console.error("获取证书信息失败:", error);
    return null;
  }
}

