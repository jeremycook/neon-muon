import { createDesigner } from './blueprint/blueprint-designer';
import { homePage } from './site/home';
import { loginPage } from './login/login';
import { logoutPage } from './login/logout';
import { registerPage } from './login/register';
import { databasePage } from './database/database';
import { pagePage } from './notebooks/page';
import { browsePage } from './files/files';

// NOTE: Route keys should be all lowercase.
export const routes: Record<string, ((params: any) => (Node | Promise<Node>))> = {
    '/': homePage,

    '/login': loginPage,
    '/logout': logoutPage,
    '/register': registerPage,

    '/browse': browsePage,

    '/database': databasePage,
    '/page': pagePage,

    '/designer': createDesigner,
}
