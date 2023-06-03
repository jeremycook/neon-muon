import { when } from '../utils/dynamicHtml';
import { ValueEvent, a, button, div, form, h1, input, label } from '../utils/html';
import { jsonPost } from '../utils/http';
import { PubT, val } from '../utils/pubSub';
import { makeUrl, redirectLocal } from '../utils/url';
import { refreshCurrentLogin } from './loginInfo';

export function login({ redirectUrl, requestElevated }: { redirectUrl?: string, requestElevated?: 't' }) {

    const data = {
        username: '',
        password: '',
        requestElevated: requestElevated?.startsWith('t'),
    };

    const errorMessage = val('');

    const view = div(
        h1('Login'),
        when(errorMessage, () => div({ class: 'text-error' }, errorMessage.val)),
        form({ async onsubmit(ev: SubmitEvent) { await onsubmit(ev, errorMessage, data, redirectUrl); } },
            div(
                label({ for: 'username' }, 'Username'),
                input({ id: 'username', required: true, autofocus: true, value: data.username, oninput(ev: ValueEvent) { data.username = ev.target.value } }),
            ),
            div(
                label({ for: 'password' }, 'Password'),
                input({ type: 'password', id: 'password', required: true, oninput(ev: ValueEvent) { data.password = ev.target.value } }),
            ),
            div({ class: 'flex flex-between' },
                button('Login'),
                a({ href: makeUrl('/register', { redirectUrl }) }, 'Register'),
            ),
        ),
    );

    return view;
}

async function onsubmit(
    ev: SubmitEvent,
    errorMessage: PubT<string>,
    data: { username: string, password: string },
    redirectUrl?: string
) {
    ev.preventDefault();

    var response = await jsonPost('/api/login', data);
    if (response.ok) {
        await refreshCurrentLogin();
        redirectLocal(redirectUrl);
        return;
    }
    else {
        errorMessage.pub(response.errorMessage ?? 'An error occured');
    }
}