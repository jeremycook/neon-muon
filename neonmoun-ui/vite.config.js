import basicSsl from '@vitejs/plugin-basic-ssl';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [
        // basicSsl(),
    ],
    server: {
        port: 52236,
        host: '127.0.0.1',
        // https: true,
        proxy: {
            '/api': {
                target: 'https://127.0.0.1:42236',
                secure: false,
            }
        }
    }
});
