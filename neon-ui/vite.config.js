import basicSsl from '@vitejs/plugin-basic-ssl';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [
        basicSsl(),
    ],
    server: {
        port: 5174,
        https: true,
        proxy: {
            '/api': {
                target: 'https://127.0.0.1:42236',
                secure: false,
            }
        }
    }
});
