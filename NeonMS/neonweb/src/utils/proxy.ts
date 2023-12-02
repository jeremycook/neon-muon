import { PubSub, _subscribe, _dispatch } from './pubSub';

/** @deprecated Experimental */

export function proxy<TValue extends object>(value: TValue): TValue & PubSub {

    const _subscriptions: WeakRef<() => (void | Promise<void>)>[] = [];

    function sub(lifetimeOwner: object, subscription: () => (void | Promise<void>)) {
        _subscribe(_subscriptions, lifetimeOwner, subscription, 'proxy subscription');
    }

    async function pub() {
        await _dispatch(_subscriptions);
    }

    const handler: ProxyHandler<TValue> = {
        get(target, p, receiver) {
            switch (p) {
                case 'pub':
                    return pub;

                case 'sub':
                    return sub;

                default:
                    return Reflect.get(target, p, receiver);
            }
        },
        set(target, p, newValue, receiver) {
            if ((target as any)[p] !== newValue) {
                const result = Reflect.set(target, p, newValue, receiver);
                pub();
                return result;
            }
            else {
                return false;
            }
        }
        // apply(target, thisArg, argArray) {
        //     console.log(target, thisArg, argArray)
        //     return Reflect.apply(target, thisArg, argArray);
        // },
    };

    const proxy1 = new Proxy(value, handler);
    return proxy1 as any;
}
