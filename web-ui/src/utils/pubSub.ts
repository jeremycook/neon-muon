export function val<TValue>(value: TValue) {
    return new Val(value);
}

export function computed<TInput, TValue>(value: PubSubT<TInput>, computation: () => TValue) {
    const comp = new Val<TValue>(computation());
    value.sub(comp, () => comp.pub(computation()));
    return comp;
}

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

export interface Pub {
    pub(): Promise<void>;
}

export interface PubT<TValue> extends Pub {
    pub(newValue?: TValue, options?: { force: boolean }): Promise<void>;
    get val(): TValue;
}

export interface Sub {
    sub(lifetimeOwner: object, subscription: () => (void | Promise<void>)): void;
}

export interface SubT<TValue> extends Sub {
    get val(): TValue;
}

export interface PubSub extends Pub, Sub { }

export interface PubSubT<TValue> extends PubT<TValue>, SubT<TValue> { }

export class Val<TValue = unknown> implements PubSubT<TValue> {
    private _subscriptions: WeakRef<() => (void | Promise<void>)>[] = [];

    constructor(private _val: TValue) { }

    public get val(): TValue {
        return this._val;
    }

    public sub(lifetimeOwner: object, subscription: () => (void | Promise<void>)) {
        _subscribe(this._subscriptions, lifetimeOwner, subscription, 'val subscription');
    }

    public async pub(newValue?: TValue, options?: { force: boolean }) {
        if (typeof newValue === 'undefined') {
            await _dispatch(this._subscriptions);
        }
        else if (options?.force || newValue !== this._val) {
            this._val = newValue;
            await _dispatch(this._subscriptions);
        }
    }
}

function _subscribe(subscriptions: WeakRef<() => (void | Promise<void>)>[], lifetimeOwner: object, subscription: () => (void | Promise<void>), description: string) {
    const sym = Symbol(description);
    (lifetimeOwner as any)[sym] = subscription;
    subscriptions.push(new WeakRef(subscription));
}

async function _dispatch(subscriptions: WeakRef<() => (void | Promise<void>)>[]) {
    const gone: number[] = [];
    let i = -1;
    for (const ref of subscriptions) {
        i += 1;

        const sub = ref.deref();
        if (sub) {
            await sub();
        }
        else {
            gone.push(i);
        }
    }
    for (const i of gone) {
        subscriptions.splice(i, 1);
    }
}
