import { jsonPut } from '../utils/http';
import { PubSubT, computed, val } from '../utils/pubSub';

export type Login = {
    auth: boolean;
    sub: string;
    name: string;
    elevated: boolean;
    roles: string[];
};

export type CurrentLogin =
    PubSubT<Readonly<Login>>

const currentLoginKey = 'currentLogin';
const loginInfoUrl = '/api/auth/current';
const guest = Object.freeze({
    auth: false,
    sub: '00000000-0000-0000-0000-000000000000',
    name: 'Guest',
    elevated: false,
    roles: [],
});

export const currentLogin: CurrentLogin = val(getCurrentLoginFromSession());
export const isAuthenticated = computed(currentLogin, () => currentLogin.val.auth);

export const refreshCurrentLogin = async () => {
    const response = await jsonPut<Login>(loginInfoUrl);
    if (response.result?.auth === true) {
        currentLogin.pub(Object.freeze(response.result));
    }
    else if (currentLogin.val !== guest) {
        currentLogin.pub(guest);
    }

    sessionStorage.setItem(currentLoginKey, JSON.stringify(currentLogin.val));
};
refreshCurrentLogin()

function getCurrentLoginFromSession(): Readonly<Login> {
    const json = sessionStorage.getItem(currentLoginKey);
    if (json) {
        const login = JSON.parse(json) as Login;
        if (login.auth) {
            return Object.freeze(login);
        }
    }

    // Fallback to guest
    return guest;
}
