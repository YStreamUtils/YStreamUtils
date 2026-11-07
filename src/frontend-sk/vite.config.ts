import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import {VitePWA} from 'vite-plugin-pwa'
const shouldOpenBrowser = process.env.VITE_BROWSER !== 'none';
const apiUrl = process.env.services__backend__http__0 || 'http://localhost:5000';
export default defineConfig({
    plugins: [
        sveltekit({
            compilerOptions: {
                runes: ({ filename }) => filename.split(/[/\\]/).includes('node_modules') ? undefined : true
            },
            adapter: adapter()
        }),
        VitePWA({
            
        })
    ],
    server: {
        host: '127.0.0.1',
        port: 5173,
        strictPort: true,
        proxy: {
            '/api': {
                target: apiUrl,
                changeOrigin: true,
                secure: false
            }
        },
        open: shouldOpenBrowser,
    },
    build: {
        outDir: "../YStreamUtils.Web/wwwroot"
    }
});