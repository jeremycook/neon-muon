import { siteCard } from '../site/siteCard';
import { h1, p } from '../utils/html';
import { jsonPost } from '../utils/http';
import { redirectLocal } from '../utils/url';
import { guest, setCurrentLogin } from './loginInfo';

export function logoutPage({ redirectUrl }: { redirectUrl?: string }) {

    const view = siteCard(
        h1('Logout'),
        p('You are being logged out.')
    );

    (async function () {
        var response = await jsonPost('/api/auth/logout');
        setCurrentLogin(guest, '');
        if (response.ok) {
            redirectLocal(redirectUrl);
            return;
        }
        else {
            // TODO: Error message, try again button, or something
        }
    })();

    return view;
}