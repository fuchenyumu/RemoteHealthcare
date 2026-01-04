import { type HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { useUserStoreHook } from "@/store/modules/user";

/**
 * 获取 API 基础地址（优先使用 public/config.js）
 */
const getApiBaseUrl = () => {
  if (window.__APP_CONFIG__?.apiBaseUrl) {
    return window.__APP_CONFIG__.apiBaseUrl;
  }
  return import.meta.env.VITE_BASE_URL || '';
};

/**
 * 返回signalr链接
 * @param url Url地址,开头要带/
 * @param directStart 直接启动
 * @returns HubConnection
 */
export const createConnection = (
  url: string,
  directStart?: boolean | undefined
): HubConnection => {
  const hubConnectionBuilder = new HubConnectionBuilder();
  // 优先使用 public/config.js 配置
  const apiBaseUrl = getApiBaseUrl();
  const signalrUrl = `${apiBaseUrl.replace(/\/$/, '')}/signalr-hubs${url}`;

  const connection = hubConnectionBuilder
    .withUrl(signalrUrl, {
      accessTokenFactory: () => useUserStoreHook().getToken
    })
    .withAutomaticReconnect()
    .build();
  if (!directStart) {
    connection.start();
  }
  return connection;
};

export const createConnectionAsync = async (
  url: string,
  directStart?: boolean | undefined
): Promise<HubConnection> => {
  const hubConnectionBuilder = new HubConnectionBuilder();
  // 优先使用 public/config.js 配置
  const apiBaseUrl = getApiBaseUrl();
  const signalrUrl = `${apiBaseUrl.replace(/\/$/, '')}/signalr-hubs${url}`;

  const connection = hubConnectionBuilder
    .withUrl(signalrUrl, {
      accessTokenFactory: () => useUserStoreHook().getToken
    })
    .withAutomaticReconnect()
    .build();
  if (!directStart) {
    await connection.start();
  }
  return connection;
};
