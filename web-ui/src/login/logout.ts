import { div, h1, p } from '../utils/html';
import { jsonPost } from '../utils/http';
import { redirectLocal } from '../utils/url';
import { refreshCurrentUser } from './loginInfo';

export function logout({ redirectUrl }: { redirectUrl?: string }) {

    const view = div(
        h1('Logout'),
        p('You are being logged out.')
    );

    (async function () {
        var response = await jsonPost('/api/logout');
        if (response.ok) {
            await refreshCurrentUser();
            redirectLocal(redirectUrl);
            return;
        }
        else {
            // TODO: Error message, try again button, or something
        }
    })();

    return view;
}