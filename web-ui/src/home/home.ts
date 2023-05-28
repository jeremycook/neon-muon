import { mutateSegment, segment } from '../utils/etc';
import { div, h1, p } from '../utils/html';
import { jsonGet } from '../utils/http';

export function home() {

    let username = segment();

    const view = div(
        h1('Welcome ', username),
        p('Please enjoy your visit.'),
    );

    (async () => {
        const user = await jsonGet<{ name: string }>('/api/user');
        mutateSegment(username, user.result?.name ?? 'guest');
    })();

    return view;
}
