import { mutateSegment, segment } from '../utils/etc';
import { ValueEvent, a, button, div, form, h1, input, label, p } from '../utils/html';
import { jsonPost } from '../utils/http';
import { makeUrl, redirectLocal } from '../utils/url';

export function register({ username, redirectUrl }: { username?: string, redirectUrl?: string }) {

    const data = {
        username: username ?? '',
        password: '',
    };

    const errorMessage = segment();

    const view = div(
        h1('Register'),
        errorMessage,
        form({ onsubmit },
            div(
                label({ for: 'username' }, 'Username'),
                input({ id: 'username', required: true, autofocus: true, value: data.username, oninput(ev: ValueEvent) { data.username = ev.target.value } }),
            ),
            div(
                label({ for: 'password' }, 'Password'),
                input({ type: 'password', id: 'password', required: true, oninput(ev: ValueEvent) { data.password = ev.target.value } }),
            ),
            div({ class: 'flex flex-between' },
                button('Register'),
                a({ href: makeUrl('/login', { redirectUrl }) }, 'Login'),
            ),
        ),
    );

    async function onsubmit(ev: SubmitEvent) {
        ev.preventDefault();

        var response = await jsonPost('/api/register', data);
        if (response.ok) {
            redirectLocal(redirectUrl);
            return;
        }
        else {
            mutateSegment(errorMessage,
                p({ class: 'text-error' },
                    response.errorMessage ?? 'An error occured'
                )
            );
        }
    }

    return view;
}