import {
  type HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HttpTransportType,
  HubConnectionState
} from "@microsoft/signalr";
import { useUserStoreHook } from "@/store/modules/user";

/**
 * 增强版 SignalR 连接创建工具
 * 支持详细日志、自动重连、错误处理
 */

interface ConnectionOptions {
  /** Hub 路径，例如：/online-user */
  url: string;
  /** 是否立即启动连接 */
  autoStart?: boolean;
  /** 是否启用详细日志（开发环境推荐） */
  enableDebugLog?: boolean;
  /** 传输方式，默认 WebSockets */
  transport?: HttpTransportType;
  /** 是否跳过协商（仅 WebSocket 时可用） */
  skipNegotiation?: boolean;
}

/**
 * 创建增强版 SignalR 连接
 */
export const createEnhancedConnection = (options: ConnectionOptions): HubConnection => {
  const {
    url,
    autoStart = false,
    enableDebugLog = import.meta.env.DEV,
    transport = HttpTransportType.WebSockets,
    skipNegotiation = false
  } = options;

  // 构建完整 URL
  const apiBaseUrl = import.meta.env.VITE_BASE_URL || "";
  const signalrUrl = `${apiBaseUrl.replace(/\/$/, "")}/signalr-hubs${url}`;

  console.log(`🔌 创建 SignalR 连接: ${signalrUrl}`);

  // 构建连接
  const connection = new HubConnectionBuilder()
    .withUrl(signalrUrl, {
      // Token 工厂函数
      accessTokenFactory: () => {
        const token = useUserStoreHook().getToken;
        if (!token) {
          console.warn("⚠️  SignalR: Token 为空，可能导致认证失败");
        }
        return token;
      },
      // 传输方式
      transport,
      // 跳过协商（仅 WebSocket）
      skipNegotiation,
      // 超时设置
      timeout: 30000 // 30秒
    })
    // 日志级别
    .configureLogging(enableDebugLog ? LogLevel.Debug : LogLevel.Warning)
    // 自动重连策略
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: retryContext => {
        const retryCount = retryContext.previousRetryCount;
        // 重连延迟：0s, 2s, 10s, 30s, 60s
        if (retryCount === 0) return 0;
        if (retryCount === 1) return 2000;
        if (retryCount === 2) return 10000;
        if (retryCount === 3) return 30000;
        return 60000;
      }
    })
    .build();

  // 连接状态监听
  setupConnectionListeners(connection, url);

  // 自动启动
  if (autoStart) {
    startConnection(connection);
  }

  return connection;
};

/**
 * 设置连接事件监听
 */
function setupConnectionListeners(connection: HubConnection, hubName: string): void {
  // 重连中
  connection.onreconnecting(error => {
    console.warn(`🔄 SignalR [${hubName}] 重连中...`, error?.message);
  });

  // 重连成功
  connection.onreconnected(connectionId => {
    console.log(`✅ SignalR [${hubName}] 重连成功! ConnectionId: ${connectionId}`);
  });

  // 连接关闭
  connection.onclose(error => {
    if (error) {
      console.error(`❌ SignalR [${hubName}] 连接关闭:`, error.message);
    } else {
      console.log(`🔌 SignalR [${hubName}] 连接正常关闭`);
    }
  });
}

/**
 * 启动连接（带错误处理）
 */
export async function startConnection(connection: HubConnection): Promise<boolean> {
  if (connection.state === HubConnectionState.Connected) {
    console.log("✅ SignalR 已连接，无需重复启动");
    return true;
  }

  try {
    await connection.start();
    console.log(`✅ SignalR 连接成功! ConnectionId: ${connection.connectionId}`);
    return true;
  } catch (error: any) {
    console.error("❌ SignalR 连接失败:", error);
    
    // 详细错误提示
    if (error.message?.includes("404")) {
      console.error("💡 提示: Hub 端点未找到，请检查后端路由配置");
      console.error("   - 确认后端使用 UseConfiguredEndpoints()");
      console.error("   - 确认 Hub 路径正确");
    } else if (error.message?.includes("401") || error.message?.includes("Unauthorized")) {
      console.error("💡 提示: 认证失败，请检查 Token");
      console.error("   - 确认用户已登录");
      console.error("   - 确认 Token 未过期");
    } else if (error.message?.includes("CORS")) {
      console.error("💡 提示: CORS 错误，请检查后端 CORS 配置");
      console.error("   - 确认 CorsOrigins 包含前端地址");
      console.error("   - 确认启用了 AllowCredentials()");
    } else if (error.message?.includes("certificate") || error.message?.includes("SSL")) {
      console.error("💡 提示: 证书错误");
      console.error("   - 在浏览器中信任自签名证书");
      console.error("   - 或运行 一键安装证书.bat");
    }
    
    return false;
  }
}

/**
 * 安全关闭连接
 */
export async function stopConnection(connection: HubConnection): Promise<void> {
  if (connection.state !== HubConnectionState.Disconnected) {
    try {
      await connection.stop();
      console.log("🔌 SignalR 连接已关闭");
    } catch (error: any) {
      console.error("❌ 关闭 SignalR 连接失败:", error.message);
    }
  }
}

/**
 * 调用 Hub 方法（带错误处理）
 */
export async function invokeHubMethod<T = any>(
  connection: HubConnection,
  methodName: string,
  ...args: any[]
): Promise<T | null> {
  try {
    if (connection.state !== HubConnectionState.Connected) {
      console.warn(`⚠️  SignalR 未连接，无法调用方法: ${methodName}`);
      return null;
    }

    const result = await connection.invoke<T>(methodName, ...args);
    console.log(`✅ Hub 方法调用成功: ${methodName}`, result);
    return result;
  } catch (error: any) {
    console.error(`❌ Hub 方法调用失败: ${methodName}`, error.message);
    return null;
  }
}

/**
 * 获取连接状态描述
 */
export function getConnectionStateText(state: HubConnectionState): string {
  const stateMap: Record<HubConnectionState, string> = {
    [HubConnectionState.Disconnected]: "已断开",
    [HubConnectionState.Connecting]: "连接中",
    [HubConnectionState.Connected]: "已连接",
    [HubConnectionState.Disconnecting]: "断开中",
    [HubConnectionState.Reconnecting]: "重连中"
  };
  return stateMap[state] || "未知状态";
}

/**
 * 兼容旧版 API（保持向后兼容）
 */
export const createConnection = (
  url: string,
  directStart?: boolean
): HubConnection => {
  return createEnhancedConnection({
    url,
    autoStart: !directStart
  });
};

export const createConnectionAsync = async (
  url: string,
  directStart?: boolean
): Promise<HubConnection> => {
  const connection = createEnhancedConnection({
    url,
    autoStart: false
  });

  if (!directStart) {
    await startConnection(connection);
  }

  return connection;
};

