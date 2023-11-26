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
                target: 'http://127.0.0.1:5295',
                // secure: false,
            }
        }
    }
});
