import { createDesigner } from './blueprint/blueprint-designer';
import { home } from './site/home';
import { login } from './login/login';
import { logout } from './login/logout';
import { register } from './login/register';
import { database } from './database/database';

export const routes: Record<string, ((params: any) => (Node | Promise<Node>))> = {
    '/': home,

    '/login': login,
    '/logout': logout,
    '/register': register,

    '/database': database,
    
    '/designer': createDesigner,
}
