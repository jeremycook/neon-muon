import basicSsl from '@vitejs/plugin-basic-ssl';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [
        basicSsl(),
    ],
    server: {
        https: true,
        proxy: {
            '/api': {
                target: 'https://localhost:7104',
                secure: false,
            }
        }
    }
});
