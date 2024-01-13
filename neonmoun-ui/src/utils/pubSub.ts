const SignalSubscriptionDescription = 'Signal subscription';

export function signal() {
    return new Signal();
}

export function val<TValue>(value: TValue) {
    return new Val(value);
}

export function computed<TValue>(sub: Sub, computation: () => TValue): SubT<TValue> {
    const comp = new Val<TValue>(computation());
    sub.sub(comp, () => {
        comp.value = computation();
    });
    return comp;
}

export function observe(...subs: Sub[]): Sub {
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

export interface Sub {
    sub(lifetimeOwner: object, subscription: () => (void | Promise<void>)): void;
}

export interface SubT<TValue> extends Sub {
    get value(): TValue;
}

export class Signal implements Pub, Sub {
    private _subscriptions: WeakRef<() => (void | Promise<void>)>[] = [];

    constructor() { }

    public sub(lifetimeOwner: object, subscription: () => (void | Promise<void>)) {
        _subscribe(this._subscriptions, lifetimeOwner, subscription, SignalSubscriptionDescription);
    }

    public async pub(): Promise<void> {
        await _dispatch(this._subscriptions);
    }
}

export class Val<TValue = unknown> extends Signal implements SubT<TValue> {
    constructor(private _value: TValue) {
        super();
    }

    get value() {
        return this._value;
    }
    set value(value: TValue) {
        if (value !== this._value) {
            this._value = value;
            this.pub();
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
