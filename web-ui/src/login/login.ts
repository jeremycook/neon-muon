import { siteCard } from '../site/siteCard';
import { when } from '../utils/dynamicHtml';
import { ValueEvent, a, button, div, form, h1, input, label, p } from '../utils/html';
import { jsonPost } from '../utils/http';
import { PubT, val } from '../utils/pubSub';
import { makeUrl, redirectLocal } from '../utils/url';
import { guest, setCurrentLogin } from './loginInfo';

export function loginPage({ redirectUrl, requestElevated }: { redirectUrl?: string, requestElevated?: 't' }) {

    const data = {
        username: '',
        password: '',
        requestElevated: requestElevated?.startsWith('t'),
    };

    const errorMessage = val('');

    const view = siteCard(
        h1('Login'),
        ...when(errorMessage, () => p({ class: 'text-error' }, errorMessage.val)),
        form({ async onsubmit(ev: SubmitEvent) { await onsubmit(ev, errorMessage, data, redirectUrl); } },
            label(
                div('Username'),
                input({ required: true, autofocus: true, autocomplete: 'username', value: data.username, oninput(ev: ValueEvent) { data.username = ev.target.value } }),
            ),
            label(
                div('Password'),
                input({ type: 'password', required: true, autocomplete: 'current-password', oninput(ev: ValueEvent) { data.password = ev.target.value } }),
            ),
            div({ class: 'flex flex-between' },
                button({ type: 'submit' }, 'Login'),
                a({ href: makeUrl('/register', { redirectUrl }) }, 'Register'),
            ),
        ),
    );

    return view;
}

async function onsubmit(
    ev: SubmitEvent,
    errorMessage: PubT<string>,
    data: { username: string; password: string },
    redirectUrl?: string
) {
    ev.preventDefault();

    var response = await jsonPost<{ token: string; notAfter: Date }>('/api/auth/login', {
        ...data,
        dataServer: 'Main',
    });
    if (response.ok) {
        setCurrentLogin({
            auth: true,
            sub: data.username,
            name: data.username,
            notAfter: response.result!.notAfter,
        }, response.result!.token);
        redirectLocal(redirectUrl);
        return;
    }
    else {
        setCurrentLogin(guest, '');
        errorMessage.pub(response.errorMessage ?? 'An error occured');
    }
}