import { loginPage } from './login/login';
import { logoutPage } from './login/logout';
import { homePage } from './site/home';

// NOTE: Route keys should be all lowercase.
export const routes: Record<string, ((params: any) => (Node | Promise<Node>))> = {
    '/': homePage,

    '/login': loginPage,
    '/logout': logoutPage,
}
