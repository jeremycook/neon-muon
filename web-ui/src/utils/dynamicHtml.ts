import { mutateSegment, Segment, segment } from './etc';
import { Val } from './Val';

export function dynamic(value: Val<string>): Segment;
export function dynamic(value: Val<any>, renderer: () => (string | Node | Promise<string | Node>)): Segment;
export function dynamic(values: Val<any>[], renderer: () => (string | Node | Promise<string | Node>)): Segment;
export function dynamic(arg0: Val<any> | Val<any>[],
    renderer?: () => (string | Node | Promise<string | Node>)): Segment {

    const seg = segment();

    const values = Array.isArray(arg0) ? arg0 : [arg0];
    if (typeof renderer === 'undefined')
        renderer = () => values[0].value;
    const subscription = async () => {
        mutateSegment(seg, await renderer!());
    };
    for (let value of values) {
        let unsub = value.sub(subscription);
        seg[0].addEventListener('onunmount', unsub);
    }

    return seg;
}
export function when(condition: Val<boolean>,
    trueRenderer: () => (string | Node | Promise<string | Node>),
    elseRenderer: () => (string | Node | Promise<string | Node>)): Segment {
    return dynamic(condition, async () => {
        if (condition.value === true) {
            return await trueRenderer();
        }
        else {
            return await elseRenderer();
        }
    });
}
