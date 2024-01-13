import { Segment, addMountEventListener, createFragment, createSegment, mutateSegment } from './etc';
import { Sub, SubT } from './pubSub';

type DynamicNode =
    (string | Node | (string | Node)[])
    | Promise<string | Node | (string | Node)[]>;

export function dynamic(value: SubT<string | Node>): Segment;
export function dynamic(sub: Sub, renderer: () => DynamicNode): Segment;
export function dynamic(arg0: Sub, renderer?: () => DynamicNode): Segment {

    const segment = createSegment();
    const end = segment[1] as HTMLTemplateElement;

    if (typeof renderer === 'undefined') {
        if ((arg0 as SubT<string | Node>)?.value) {
            // Assuming .val is valid
            renderer = () => (arg0 as SubT<string | Node>).value;
        }
        else {
            throw new Error('A renderer was not provided and could not be inferred.');
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

    // React to dependency
    arg0.sub(end, subscription);

    // Trigger the subscription when this element is mounted
    addMountEventListener(end, subscription);

    return segment;
}
export function lazy(promise: Promise<Node | Node[]>, loading?: Node): Segment;
export function lazy(renderer: () => Promise<Node | Node[]>, loading?: Node): Segment;
export function lazy(renderer: Promise<Node | Node[]> | (() => Promise<Node | Node[]>), loading?: Node): Segment {
    const segment = loading ? createSegment(loading) : createSegment();

    const promise = renderer instanceof Promise
        ? renderer
        : renderer();
    promise.then(node => Array.isArray(node)
        ? mutateSegment(segment, ...node)
        : mutateSegment(segment, node)
    );

    return segment;
}

export function when(condition: SubT<any>,
    truthyRenderer: () => (string | Node | Promise<string | Node>),
    elseRenderer?: () => (string | Node | Promise<string | Node>)): Segment {
    return dynamic(condition, async () => {
        if (condition.value) {
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
