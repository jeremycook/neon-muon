import { Val } from '../utils/Val';
import { jsonGet } from '../utils/http'

const guest = Object.freeze({
    auth: false,
    name: 'Guest',
    sub: '00000000-0000-0000-0000-000000000000',
});

export const currentUser = new Val<Readonly<{ auth: boolean, name: string, sub: string }>>(guest)

export const refreshCurrentUser = async () => {
    const response = await jsonGet<{ auth: boolean; name: string; sub: string; }>('/api/user')
    if (response.result?.auth == true) {
        currentUser.pub(Object.freeze(response.result))
    }
    else if (currentUser.value !== guest) {
        currentUser.pub(guest)
    }
};

refreshCurrentUser()
