import { siteCard } from '../site/siteCard';
import { when } from '../utils/dynamicHtml';
import { ValueEvent, a, button, div, form, h1, input, label, p } from '../utils/html';
import { jsonPost } from '../utils/http';
import { val } from '../utils/pubSub';
import { makeUrl, redirectLocal } from '../utils/url';

export function registerPage({ username, redirectUrl }: { username?: string, redirectUrl?: string }) {

    const data = {
        username: username ?? '',
        password: '',
    };

    const errorMessage = val('');

    const view = siteCard(
        h1('Register'),
        ...when(errorMessage, () => p({ class: 'text-error' }, errorMessage.val)),
        form({ onsubmit },
            div(
                label({ for: 'username' }, 'Username'),
                input({ required: true, autocomplete: 'username', autofocus: true, value: data.username, oninput(ev: ValueEvent) { data.username = ev.target.value } }),
            ),
            div(
                label({ for: 'password' }, 'Password'),
                input({ type: 'password', required: true, minLength: 10, autocomplete: 'new-password', oninput(ev: ValueEvent) { data.password = ev.target.value } }),
            ),
            div({ class: 'flex flex-between' },
                button({ type: 'submit' }, 'Register'),
                a({ href: makeUrl('/login', { redirectUrl }) }, 'Login'),
            ),
        ),
    );

    async function onsubmit(ev: SubmitEvent) {
        ev.preventDefault();

        var response = await jsonPost('/api/register', data);
        if (response.ok) {
            redirectLocal(makeUrl('/login', { redirectUrl }));
            return;
        }
        else {
            errorMessage.pub(response.errorMessage ?? 'An error occurred.');
        }
    }

    return view;
}