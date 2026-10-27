import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// `/api` is proxied to the server rather than called across origins, so the browser treats the two as one site
// and CORS never enters into development. The API's CORS configuration is there for a deployment that does
// serve them from two origins.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": { target: "http://localhost:5080", changeOrigin: true },
    },
  },
});
