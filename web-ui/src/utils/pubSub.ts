export function val<TValue>(value: TValue) {
    return new Val(value);
}

export interface PubSub {
    sub(subscription: () => (void | Promise<void>)): () => void;
    pub(): Promise<void>;
}

export interface SubT<TValue> extends PubSub {
    pub(newValue?: TValue): Promise<void>;
    get val(): TValue;
}

export class Val<TValue = unknown> implements SubT<TValue> {
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
