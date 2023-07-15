import { currentLogin } from '../login/loginInfo'
import { a, h1, p } from '../utils/html'
import { dynamic } from '../utils/dynamicHtml'
import { siteCard } from './siteCard'

export async function homePage() {

    const view = siteCard(
        h1(dynamic(currentLogin, () => 'Welcome ' + currentLogin.val.name)),
        p('Enjoy your visit.'),
        p(dynamic(currentLogin, () => currentLogin.val.auth
            ? a({ href: '/logout' }, 'Logout')
            : a({ href: '/login' }, 'Login')
        )),
    );

    return view;
}
