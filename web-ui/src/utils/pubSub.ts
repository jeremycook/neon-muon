export function val<TValue>(value: TValue) {
    return new Val(value);
}

export function computed<TInput, TValue>(value: PubSubT<TInput>, computation: () => TValue) {
    const comp = new Val<TValue>(computation());

    // Note that the unsubscribe is discarded
    value.sub(() => comp.pub(computation()));

    return comp;
}

export interface Pub {
    pub(): Promise<void>;
}

export interface PubT<TValue> extends Pub {
    pub(newValue?: TValue): Promise<void>;
    get val(): TValue;
}

export interface Sub {
    sub(subscription: () => (void | Promise<void>)): () => void;
}

export interface SubT<TValue> extends Sub {
    get val(): TValue;
}

export interface PubSub extends Pub, Sub { }

export interface PubSubT<TValue> extends PubT<TValue>, SubT<TValue> { }

export class Val<TValue = unknown> implements PubSubT<TValue> {
    private _subscriptions: (() => (void | Promise<void>))[] = [];

    constructor(private _val: TValue) { }

    public get val(): TValue {
        return this._val;
    }

    public sub(subscription: () => (void | Promise<void>)) {
        this._subscriptions.push(subscription);
        return (): void => { this._subscriptions.splice(this._subscriptions.indexOf(subscription, 1)) };
    }

    public async pub(newValue?: TValue) {
        if (typeof newValue === 'undefined') {
            await this._dispatch();
        }
        else if (newValue !== this._val) {
            this._val = newValue;
            await this._dispatch();
        }
    }

    private async _dispatch() {
        for (const sub of this._subscriptions) {
            await sub();
        }
    }
}
