import { mutateSegment, Segment, segment } from '../utils/etc';
import { a, div, h1, p } from '../utils/html';
import { jsonGet } from '../utils/http';
import { Val } from '../utils/Val';

function dynamic(value: Val<string>): Segment;
function dynamic(value: Val<any>, renderer: () => (string | Node | Promise<string | Node>)): Segment;
function dynamic(values: Val<any>[], renderer: () => (string | Node | Promise<string | Node>)): Segment;
function dynamic(arg0: Val<any> | Val<any>[],
    renderer?: () => (string | Node | Promise<string | Node>)): Segment {
    const seg = segment()

    const values = Array.isArray(arg0) ? arg0 : [arg0]
    if (typeof renderer === 'undefined') renderer = () => values[0].value
    const subscription = async () => {
        mutateSegment(seg, await renderer!())
    };
    for (let value of values) {
        let unsub = value.sub(subscription)
        seg[0].addEventListener('onunmount', unsub)
    }

    return seg
}

function when(condition: Val<boolean>,
    trueRenderer: () => (string | Node | Promise<string | Node>),
    elseRenderer: () => (string | Node | Promise<string | Node>)): Segment {
    return dynamic(condition, async () => {
        if (condition.value === true) {
            return await trueRenderer()
        }
        else {
            return await elseRenderer()
        }
    })
}

export async function home() {

    const authenticated = new Val(false);
    const name = new Val('Guest');

    const view = div(
        h1('Welcome ', ...dynamic(name)),
        p('Please enjoy your visit.'),
        p(when(authenticated,
            () => a({ href: '/logout' }, 'Logout'),
            () => a({ href: '/login' }, 'Login')
        )),
    );

    // Fetch user info
    const response = await jsonGet<{ auth: boolean, name: string, sub: string }>('/api/user');
    if (response.result) {
        authenticated.pub(response.result.auth);
        name.pub(response.result.auth
            ? response.result.name
            : 'Guest');
    }

    return view;
}
