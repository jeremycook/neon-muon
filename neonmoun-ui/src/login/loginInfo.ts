import { jsonPut, setBearerToken } from '../utils/http';
import { Val, computed, val } from '../utils/pubSub';

export type Login = {
    auth: boolean;
    sub: string;
    name: string;
    notAfter: Date;
};

export type CurrentLogin =
    Val<Readonly<Login>>

const currentLoginKey = 'currentLogin';
const loginInfoUrl = '/api/auth/current';
export const guest = Object.freeze({
    auth: false,
    sub: '?',
    name: 'Guest',
    notAfter: new Date(),
});

export const currentLogin: CurrentLogin = val(getCurrentLoginFromSession());
export const isAuthenticated = computed(currentLogin, () => currentLogin.value.auth);

export function setCurrentLogin(login: Readonly<Login>, token: string) {
    setBearerToken(token);

    if (login.auth === true) {
        currentLogin.value = Object.freeze(login);
    }
    else {
        currentLogin.value = guest;
    }

    sessionStorage.setItem(currentLoginKey, JSON.stringify(currentLogin.value));
}

export const refreshCurrentLogin = async () => {
    const response = await jsonPut<Login>(loginInfoUrl);
    if (response.result?.auth === true) {
        currentLogin.value = Object.freeze(response.result);
    }
    else {
        currentLogin.value = guest;
    }

    sessionStorage.setItem(currentLoginKey, JSON.stringify(currentLogin.value));
};
// TODO? refreshCurrentLogin()

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
