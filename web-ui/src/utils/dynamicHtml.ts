import { mutateSegment, Segment, createSegment, createFragment } from './etc';
import { Exception } from './exceptions';
import { Sub, SubT } from './pubSub';

type DynamicNode =
    (string | Node | (string | Node)[])
    | Promise<string | Node | (string | Node)[]>;

export function dynamic(value: SubT<string | Node>): Segment;
export function dynamic(sub: Sub, renderer: () => DynamicNode): Segment;
export function dynamic(arg0: Sub, renderer?: () => DynamicNode): Segment {

    const segment = createSegment();
    const end = segment[1] as Comment;

    if (typeof renderer === 'undefined') {
        if ((arg0 as SubT<string | Node>)?.val) {
            // Assuming .val is valid
            renderer = () => (arg0 as SubT<string | Node>).val;
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

    // React to dependency
    arg0.sub(end, subscription);

    // Trigger the subscription when this element is mounted
    end.addEventListener('mount', subscription);

    return segment;
}

export function lazy(renderer: () => Promise<Node | Node[]>) {
    const segment = createSegment();
    renderer().then(node => Array.isArray(node)
        ? mutateSegment(segment, ...node)
        : mutateSegment(segment, node)
    );
    return segment;
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
