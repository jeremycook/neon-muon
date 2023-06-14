export function val<TValue>(value: TValue) {
    return new Val(value);
}

export function computed<TInput, TValue>(value: PubSubT<TInput>, computation: () => TValue) {
    const comp = new Val<TValue>(computation());
    value.sub(() => comp.pub(computation()));
    return comp;
}

/** @deprecated Experimental */
export function proxy<TValue extends object>(value: TValue): TValue & PubSub {

    const _pointers: Symbol[] = [];
    const _subscriptions = new WeakMap<Symbol, () => (void | Promise<void>)>();

    function sub(subscription: () => (void | Promise<void>)) {
        const ptr = Symbol();
        _pointers.push(ptr);
        _subscriptions.set(ptr, subscription);
    }

    async function pub() {
        await _dispatch();
    }

    async function _dispatch() {
        for (const ptr of _pointers) {
            const sub = _subscriptions.get(ptr);
            if (sub) {
                await sub();
            }
            else {
                // TODO: Do we care about cleaning up dangling pointers
            }
        }
    }

    const handler: ProxyHandler<TValue> = {
        get(target, p, receiver) {
            console.log(target, p, receiver)
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
            console.log(target, p, newValue, receiver)
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
    sub(subscription: () => (void | Promise<void>)): void;
}

export interface SubT<TValue> extends Sub {
    get val(): TValue;
}

export interface PubSub extends Pub, Sub { }

export interface PubSubT<TValue> extends PubT<TValue>, SubT<TValue> { }

export class Val<TValue = unknown> implements PubSubT<TValue> {
    private _pointers: Symbol[] = [];
    private _subscriptions = new WeakMap<Symbol, () => (void | Promise<void>)>();

    constructor(private _val: TValue) { }

    public get val(): TValue {
        return this._val;
    }

    public sub(subscription: () => (void | Promise<void>)) {
        const ptr = Symbol();
        this._pointers.push(ptr);
        this._subscriptions.set(ptr, subscription);
    }

    public async pub(newValue?: TValue, options?: { force: boolean }) {
        if (typeof newValue === 'undefined') {
            await this._dispatch();
        }
        else if (options?.force || newValue !== this._val) {
            this._val = newValue;
            await this._dispatch();
        }
    }

    private async _dispatch() {
        for (const ptr of this._pointers) {
            const sub = this._subscriptions.get(ptr);
            if (sub) {
                await sub();
            }
            else {
                // TODO: Do we care about cleaning up dangling pointers
            }
        }
    }
}
