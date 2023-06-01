import { val } from '../utils/pubSub';
import { jsonGet } from '../utils/http'

type CurrentLogin = {
    auth: boolean;
    name: string;
    sub: string;
};

const currentLoginKey = 'currentLogin';
const loginInfoUrl = '/api/login-info';
const guest = Object.freeze({
    auth: false,
    name: 'Guest',
    sub: '00000000-0000-0000-0000-000000000000',
});

export const currentLogin = val(getCurrentLoginFromSession());

export const refreshCurrentLogin = async () => {
    const response = await jsonGet<CurrentLogin>(loginInfoUrl);
    if (response.result?.auth === true) {
        currentLogin.pub(Object.freeze(response.result));
    }
    else if (currentLogin.val !== guest) {
        currentLogin.pub(guest);
    }

    sessionStorage.setItem(currentLoginKey, JSON.stringify(currentLogin.val));
};
refreshCurrentLogin()

function getCurrentLoginFromSession(): Readonly<CurrentLogin> {
    const json = sessionStorage.getItem(currentLoginKey);
    if (json) {
        const login = JSON.parse(json) as CurrentLogin;
        if (login.auth) {
            return Object.freeze(login);
        }
    }

    // Fallback to guest
    return guest;
}
