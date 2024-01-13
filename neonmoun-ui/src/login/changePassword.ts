import { siteCard } from '../site/siteCard';
import { when } from '../utils/dynamicHtml';
import { ValueEvent, button, div, form, h1, input, label, p } from '../utils/html';
import { jsonPost } from '../utils/http';
import { Val, val } from '../utils/pubSub';
import { redirectLocal } from '../utils/url';
import { refreshCurrentLogin } from './loginInfo';

export function changePasswordPage({ redirectUrl }: { redirectUrl?: string }) {

    const data = {
        username: '',
        password: '',
        newPassword: '',
    };

    const errorMessage = val('');

    const view = siteCard(
        h1('Change Password'),
        ...when(errorMessage, () => p({ class: 'text-error' }, errorMessage.value)),
        form({ async onsubmit(ev: SubmitEvent) { await onsubmit(ev, errorMessage, data, redirectUrl); } },
            label(
                div('Username'),
                input({ required: true, autofocus: true, autocomplete: 'username', value: data.username, oninput(ev: ValueEvent) { data.username = ev.target.value } }),
            ),
            label(
                div('Current Password'),
                input({ type: 'password', required: true, autocomplete: 'current-password', oninput(ev: ValueEvent) { data.password = ev.target.value } }),
            ),
            label(
                div('New Password'),
                input({ type: 'password', required: true, minLength: 10, autocomplete: 'new-password', oninput(ev: ValueEvent) { data.newPassword = ev.target.value } }),
            ),
            div({ class: 'flex flex-between' },
                button({ type: 'submit' }, 'Change Password'),
            ),
        ),
    );

    return view;
}

async function onsubmit(
    ev: SubmitEvent,
    errorMessage: Val<string>,
    data: { username: string, password: string },
    redirectUrl?: string
) {
    ev.preventDefault();

    var response = await jsonPost('/api/change-password', data);
    if (response.ok) {
        await refreshCurrentLogin();
        redirectLocal(redirectUrl, '/login');
        return;
    }
    else {
        errorMessage.value = response.errorMessage || 'An error occured.';
    }
}