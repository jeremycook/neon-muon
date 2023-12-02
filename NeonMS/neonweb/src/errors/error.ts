import { siteCard } from '../site/siteCard';
import { a, h1, p } from '../utils/html';

export function errorPage() {

    const view = siteCard(
        h1('Unexpected Error'),
        p(
            'An error occurred that is preventing this page from working. ',
            a({ onclick: 'javascript:location.reload()' }, 'Reload this page. '),
            a({ href: '/' }, 'Return home. ')
        )
    );

    return view;
}