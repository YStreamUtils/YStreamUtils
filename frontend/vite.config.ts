import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import path from 'path';

const shouldOpenBrowser = process.env.VITE_BROWSER !== 'none';
export default defineConfig({
  base: './',
  plugins: [svelte()],
  build: {
    outDir: path.resolve(import.meta.dirname, '../YStreamUtils/wwwroot'),
    emptyOutDir: true
  },
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false
      }
    },
    open: shouldOpenBrowser,
  },
  resolve: {
    alias: {
      $lib: path.resolve(__dirname, './src/lib')
    }
  }
});
