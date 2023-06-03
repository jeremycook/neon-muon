import { CurrentLogin, Login } from '../login/loginInfo';
import icon from '../ui/icons';
import { dynamic } from '../utils/dynamicHtml';
import { a, button, div, small, span } from '../utils/html';

export function siteNavbarUI(login: CurrentLogin) {
    return dynamic(login, () => login.val.auth
        ? AuthenticatedNavbar(login.val)
        : AnonymousNavbar()
    )
}

function AuthenticatedNavbar(login: Login) {
    return div({ class: 'navbar' },
        div({ class: 'navbar-group' }),
        div({ class: 'navbar-group' }),
        div({ class: 'navbar-group' },
            a({ class: 'navbar-item', href: '/' },
                icon('home-12-regular'),
                span({ class: 'sr-only' }, 'Home'),
            ),
            button({ class: 'navbar-item', onclick: () => toggleNotifications() },
                icon('alert-12-regular'),
                span({ class: 'sr-only' }, 'Notifications'),
            ),
            div({ class: 'navbar-item dropdown' },
                button({ type: 'button', id: 'site-navbar-user-dropdown' },
                    icon('person-12-regular'),
                    span({ class: 'sr-only' }, 'Profile'),
                ),
                div({ class: 'dropdown-anchor', 'aria-labelledby': 'site-navbar-user-dropdown' },
                    div({ class: 'dropdown-content' },
                        small(`Hi ${login.name}`),
                        a({ href: '/my' }, 'My Profile'),
                        a({ href: '/logout' }, 'Logout'),
                    ),
                )
            )
        ),
    );
}

function AnonymousNavbar() {
    return div({ class: 'navbar-trio' },
        div({ class: 'navbar-group' }),
        div({ class: 'navbar-group' },
            a({ class: 'navbar-item', href: '/' },
                icon('home-12-regular'), ' Home',
            ),
            a({ class: 'navbar-item', href: '/login' },
                icon('person-12-regular'), ' Login',
            ),
            a({ class: 'navbar-item', href: '/join' },
                icon('sparkle-16-regular'), ' Join',
            ),
        ),
        div({ class: 'navbar-group' }),
    );
}

function toggleNotifications() {
    throw new Error('toggleNotifications function not implemented.');
}
