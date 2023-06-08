import { createDesigner } from './blueprint/blueprint-designer';
import { homePage } from './site/home';
import { loginPage } from './login/login';
import { logoutPage } from './login/logout';
import { registerPage } from './login/register';
import { browsePage } from "./files/browse";

// NOTE: Route keys should be all lowercase.
export const routes: Record<string, ((params: any) => (Node | Promise<Node>))> = {
    '/': homePage,

    '/login': loginPage,
    '/logout': logoutPage,
    '/register': registerPage,

    '/browse': browsePage,

    '/designer': createDesigner,
}
