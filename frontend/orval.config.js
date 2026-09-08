import { defineConfig } from 'orval';

export default defineConfig({
    ystreamApi: {
        input: '../openapi.json',
        output: {
            target: './src/lib/api.ts',
            client: 'fetch',
        },
    },
});
