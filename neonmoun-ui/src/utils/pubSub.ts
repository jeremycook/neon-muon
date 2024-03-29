const SigSubscriptionDescription = 'Sig';

export function sig() {
    return new Sig();
}

export function val<TValue>(value: TValue) {
    return new Val(value);
}

export function computed<TValue>(sub: Sub, computation: () => TValue): SubValue<TValue> {
    const comp = new Val<TValue>(computation());
    sub.sub(comp, () => {
        comp.value = computation();
    });
    return comp;
}

export function observe(...subs: Sub[]): Sub {
    const observer = sig();
    const subscription = () => observer.pub();
    for (const sub of subs) {
        sub.sub(observer, subscription);
    }
    return observer;
}

export interface Pub {
    pub(): void;
}

export interface Sub {
    sub(lifetimeOwner: object, subscription: () => void): void;
}

export interface SubValue<TValue> extends Sub {
    get value(): TValue;
}

export class Sig implements Pub, Sub {
    private _subscriptions: WeakRef<() => void>[] = [];

    constructor() { }

    public sub(lifetimeOwner: object, subscription: () => void) {
        _subscribe(this._subscriptions, lifetimeOwner, subscription, SigSubscriptionDescription);
    }

    public pub(): void {
        _dispatch(this._subscriptions);
    }
}

export class Val<TValue = unknown> extends Sig implements SubValue<TValue> {
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

function _subscribe(
    subscriptions: WeakRef<() => void>[],
    lifetimeOwner: object, subscription: () => void,
    description: string
) {
    const sym = Symbol(description);
    (lifetimeOwner as any)[sym] = subscription;
    subscriptions.push(new WeakRef(subscription));
}

async function _dispatch(subscriptions: WeakRef<() => void>[]) {
    const gone: number[] = [];
    let i = -1;
    for (const ref of subscriptions) {
        i += 1;

        const sub = ref.deref();
        if (sub) {
            sub();
        }
        else {
            gone.push(i);
        }
    }
    for (const i of gone) {
        subscriptions.splice(i, 1);
    }
}
