import adapter from '@sveltejs/adapter-static';
import {sveltekit} from '@sveltejs/kit/vite';
import {defineConfig} from 'vite';

const shouldOpenBrowser = process.env.VITE_BROWSER !== 'none';

export default defineConfig({
    plugins: [
        sveltekit({
            compilerOptions: {
                runes: ({filename}) => filename.split(/[/\\]/).includes('node_modules') ? undefined : true
            },
            adapter: adapter()
        })
    ],
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
});
