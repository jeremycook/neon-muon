import { currentLogin } from '../login/loginInfo'
import { a, div, h1, p } from '../utils/html'
import { dynamic } from '../utils/dynamicHtml'

export async function home() {

    const view = div(
        h1('Welcome ', ...dynamic(currentLogin, () => currentLogin.val.name)),
        p('Please enjoy your visit.'),
        p(dynamic(currentLogin, () => currentLogin.val.auth
            ? a({ href: '/logout' }, 'Logout')
            : a({ href: '/login' }, 'Login')
        )),
    )

    return view
}
