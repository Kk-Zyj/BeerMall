import { defineConfig } from "vite";
import uni from "@dcloudio/vite-plugin-uni";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [uni()],
  css: {
    preprocessorOptions: {
      scss: {
        // 这一行非常关键：自动在所有 scss 文件头部注入 theme.scss
        additionalData: `@import "uview-plus/theme.scss";`
      },
    },
  },
});
