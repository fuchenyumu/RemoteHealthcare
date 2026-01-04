import { getPluginsList } from "./build/plugins";
import { include, exclude } from "./build/optimize";
import { type UserConfigExport, type ConfigEnv, loadEnv } from "vite";
import {
  root,
  alias,
  wrapperEnv,
  pathResolve,
  __APP_INFO__
} from "./build/utils";
import fs from "fs";
import path from "path";

export default ({ mode }: ConfigEnv): UserConfigExport => {
  const env = wrapperEnv(loadEnv(mode, root));
  const { VITE_CDN, VITE_PORT, VITE_COMPRESSION, VITE_PUBLIC_PATH, VITE_HTTPS_ENABLED } = env;

  return {
    base: VITE_PUBLIC_PATH,
    root,
    resolve: {
      alias
    },
    // 服务端渲染
    server: {
      // 端口号
      port: VITE_PORT,
      host: "0.0.0.0",
      // 启用HTTPS，使用自签名证书
      https: VITE_HTTPS_ENABLED ? (() => {
        const certPath = path.resolve(root, "dev-cert.pem");
        const keyPath = path.resolve(root, "dev-key.pem");

        // 检查证书文件是否存在
        if (fs.existsSync(certPath) && fs.existsSync(keyPath)) {
          console.log("✅ HTTPS 已启用，使用证书:", certPath);
          return {
            cert: fs.readFileSync(certPath),
            key: fs.readFileSync(keyPath)
          };
        } else {
          console.warn(
            "⚠️  警告: 未找到证书文件 (dev-cert.pem, dev-key.pem)\n" +
            "请运行 generate-cert.bat 生成证书\n" +
            "当前使用 Vite 默认证书"
          );
          return true; // 回退到默认证书
        }
      })() : undefined,
      // 本地跨域代理配置
      // 使用代理可以避免CORS问题，因为代理服务器与后端是同源的
      proxy: {
        '/api': {
          target: 'https://192.168.1.113:8828',
          changeOrigin: true,
          secure: false, // 自签名证书
          rewrite: (path) => path.replace(/^\/api/, '/api')
        },
        '/signalr-hubs': {
          target: 'https://192.168.1.113:8828',
          changeOrigin: true,
          ws: true, // WebSocket支持
          secure: false, // 自签名证书
          rewrite: (path) => path
        }
      },
      // 预热文件以提前转换和缓存结果，降低启动期间的初始页面加载时长并防止转换瀑布
      warmup: {
        clientFiles: ["./index.html", "./src/{views,components}/*"]
      }
    },
    plugins: getPluginsList(VITE_CDN, VITE_COMPRESSION),
    // https://cn.vitejs.dev/config/dep-optimization-options.html#dep-optimization-options
    optimizeDeps: {
      include,
      exclude
    },
    build: {
      // https://cn.vitejs.dev/guide/build.html#browser-compatibility
      target: "es2015",
      sourcemap: false,
      // 消除打包大小超过500kb警告
      chunkSizeWarningLimit: 4000,
      rollupOptions: {
        input: {
          index: pathResolve("./index.html", import.meta.url)
        },
        // 静态资源分类打包
        output: {
          chunkFileNames: "static/js/[name]-[hash].js",
          entryFileNames: "static/js/[name]-[hash].js",
          assetFileNames: "static/[ext]/[name]-[hash].[ext]"
        }
      }
    },
    define: {
      __INTLIFY_PROD_DEVTOOLS__: false,
      __APP_INFO__: JSON.stringify(__APP_INFO__)
    }
  };
};
