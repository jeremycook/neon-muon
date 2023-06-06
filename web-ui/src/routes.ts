import { createDesigner } from './blueprint/blueprint-designer';
import { homePage } from './site/home';
import { loginPage } from './login/login';
import { logoutPage } from './login/logout';
import { registerPage } from './login/register';
import { databasePage } from './database/database';

export const routes: Record<string, ((params: any) => (Node | Promise<Node>))> = {
    '/': homePage,

    '/login': loginPage,
    '/logout': logoutPage,
    '/register': registerPage,

    '/database': databasePage,
    
    '/designer': createDesigner,
}
