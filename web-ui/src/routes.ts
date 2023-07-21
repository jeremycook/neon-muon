import { createDesigner } from './blueprint/blueprint-designer';
import { browsePage } from "./files/browse";
import { changePasswordPage } from './login/changePassword';
import { loginPage } from './login/login';
import { logoutPage } from './login/logout';
import { registerPage } from './login/register';
import { homePage } from './site/home';

// NOTE: Route keys should be all lowercase.
export const routes: Record<string, ((params: any) => (Node | Promise<Node>))> = {
    '/': homePage,

    '/change-password': changePasswordPage,
    '/login': loginPage,
    '/logout': logoutPage,
    '/register': registerPage,

    '/browse': browsePage,

    '/designer': createDesigner,
}
