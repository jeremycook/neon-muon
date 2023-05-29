import { Segment, mutateSegment, segment } from '../utils/etc';
import { ValueEvent, a, button, div, form, h1, input, label, p } from '../utils/html';
import { jsonPost } from '../utils/http';
import { makeUrl, redirectLocal } from '../utils/url';
import { refreshCurrentUser } from './loginInfo';

export function login({ redirectUrl }: { redirectUrl?: string }) {

    const data = {
        username: '',
        password: '',
    };

    let errorMessage: Segment;
    let registerLink: HTMLAnchorElement;

    const view = div(
        h1('Login'),
        errorMessage = segment(),
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
                button('Login'),
                registerLink = a({ href: makeUrl('/register', { redirectUrl }) }, 'Register'),
            ),
        ),
    );

    async function onsubmit(ev: SubmitEvent) {
        ev.preventDefault();

        var response = await jsonPost('/api/login', data);
        if (response.ok) {
            await refreshCurrentUser();
            redirectLocal(redirectUrl);
            return;
        }
        else {
            mutateSegment(errorMessage,
                p({ class: 'text-error' },
                    response.errorMessage ?? 'An error occured'
                )
            );
            registerLink.href = makeUrl('/register', { redirectUrl, username: data.username });
        }
    }

    return view;
}