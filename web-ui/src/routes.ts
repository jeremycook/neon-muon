import { createDesigner } from './blueprint/blueprint-designer';
import { home } from './home/home';
import { login } from './login/login';
import { logout } from './login/logout';
import { register } from './login/register';

export const routes: Record<string, ((params: any) => (Node | Promise<Node>))> = {
    '/': home,
    '/login': login,
    '/logout': logout,
    '/register': register,
    '/designer': createDesigner,
}
