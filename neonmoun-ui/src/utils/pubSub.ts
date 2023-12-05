export function val<TValue>(value: TValue) {
    return new Val(value);
}

export function computed<TInput, TValue>(value: PubSubT<TInput>, computation: () => TValue) {
    const comp = new Val<TValue>(computation());
    value.sub(comp, () => comp.pub(computation()));
    return comp;
}

export function observe(...subs: Sub[]): PubSub {
    const observer = val(null);
    const subscription = () => observer.pub();
    for (const sub of subs) {
        sub.sub(observer, subscription);
    }
    return observer;
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

export function _subscribe(subscriptions: WeakRef<() => (void | Promise<void>)>[], lifetimeOwner: object, subscription: () => (void | Promise<void>), description: string) {
    const sym = Symbol(description);
    (lifetimeOwner as any)[sym] = subscription;
    subscriptions.push(new WeakRef(subscription));
}

export async function _dispatch(subscriptions: WeakRef<() => (void | Promise<void>)>[]) {
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
