import { mutateSegment, segment } from '../utils/etc';
import { a, div, h1, p } from '../utils/html';
import { jsonGet } from '../utils/http';
import { Val } from '../utils/Val';

export async function home() {

    const authenticated = new Val(false);
    const name = new Val('Guest');

    const username = segment();
    const login = segment();
    const view = div(
        h1('Welcome ', username),
        p('Please enjoy your visit.'),
        p(login),
    );

    name.sub(() => mutateSegment(username, name.value));
    authenticated.sub(() => {
        if (authenticated.value) {
            mutateSegment(login, a({ href: '/logout' }, 'Logout'));
        }
        else {
            mutateSegment(login, a({ href: '/login' }, 'Login'));
        }
    });

    await (async function fetchUserInfo() {
        const response = await jsonGet<{ auth: boolean, name: string, sub: string }>('/api/user');
        if (response.result) {
            authenticated.pub(response.result.auth);
            name.pub(response.result.auth
                ? response.result.name
                : 'Guest');
        }
    })();

    return view;
}
