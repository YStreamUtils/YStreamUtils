import { defineConfig } from 'orval';

export default defineConfig({
    api: {
        input: '../openapi.json',
        output: {
            target: './src/lib/api.ts',
            client: 'fetch',
            override: {
                useNativeEnums: true,
            },
        },
    },
});
