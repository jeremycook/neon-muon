import { mutateSegment, Segment, createSegment, createFragment } from './etc';
import { Exception } from './exceptions';
import { PubSubT, Sub, SubT } from './pubSub';

type DynamicNode =
    (string | Node | (string | Node)[])
    | Promise<string | Node | (string | Node)[]>;

export function dynamic(value: SubT<string | Node>): Segment;
export function dynamic(sub: Sub, renderer: () => DynamicNode): Segment;
export function dynamic(subs: Sub[], renderer: () => DynamicNode): Segment;
export function dynamic(arg0: Sub | Sub[], renderer?: () => DynamicNode): Segment {

    const segment = createSegment();
    const begin = segment[0];

    if (typeof renderer === 'undefined') {
        if ((arg0 as PubSubT<string | Node>)?.val) {
            // Assuming .val is valid
            renderer = () => (arg0 as PubSubT<string | Node>).val;
        }
        else {
            throw new Exception('A renderer was not provided and could not be inferred.');
        }
    }

    const subscription = async () => {
        const result = renderer!();
        if (Array.isArray(result)) {
            mutateSegment(segment, ...result);
        }
        else if (result instanceof Promise) {
            const resolved = await result;
            if (Array.isArray(resolved)) {
                mutateSegment(segment, ...resolved);
            }
            else {
                mutateSegment(segment, resolved);
            }
        }
        else {
            mutateSegment(segment, result);
        }
    };

    if (!Array.isArray(arg0)) {
        begin.addEventListener('mount', () => {
            const unsub = arg0.sub(subscription);
            begin.addEventListener('unmount', unsub);
            subscription();
        });

        return segment;
    }
    else {
        begin.addEventListener('mount', () => {
            for (let value of arg0) {
                let unsub = value.sub(subscription);
                segment[0].addEventListener('unmount', unsub);
            }
            subscription();
        });

        return segment;
    }
}

export function when(condition: SubT<any>,
    truthyRenderer: () => (string | Node | Promise<string | Node>),
    elseRenderer?: () => (string | Node | Promise<string | Node>)): Segment {
    return dynamic(condition, async () => {
        if (condition.val) {
            return await truthyRenderer();
        }
        else if (elseRenderer) {
            return await elseRenderer();
        }
        else {
            return createFragment();
        }
    });
}
