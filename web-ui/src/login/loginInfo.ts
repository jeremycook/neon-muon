import { val } from '../utils/pubSub';
import { jsonGet } from '../utils/http'

type CurrentLogin = {
    auth: boolean;
    name: string;
    sub: string;
};

const CURRENT_LOGIN_KEY = 'loginInfo.currentLogin';
const guest = Object.freeze({
    auth: false,
    name: 'Guest',
    sub: '00000000-0000-0000-0000-000000000000',
});

export const currentLogin = val(getCurrentLoginFromSession());

export const refreshCurrentLogin = async () => {
    const response = await jsonGet<CurrentLogin>('/api/user');
    if (response.result?.auth == true) {
        currentLogin.pub(Object.freeze(response.result));
    }
    else if (currentLogin.val !== guest) {
        currentLogin.pub(guest);
    }

    sessionStorage.setItem(CURRENT_LOGIN_KEY, JSON.stringify(currentLogin.val));
};
refreshCurrentLogin()

function getCurrentLoginFromSession(): Readonly<CurrentLogin> {
    const json = sessionStorage.getItem(CURRENT_LOGIN_KEY);
    if (json) {
        const login = JSON.parse(json) as CurrentLogin;
        if (login.auth) {
            return Object.freeze(login);
        }
    }

    // Fallback to guest
    return guest;
}
