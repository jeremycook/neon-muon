import { a, div, h1, p } from '../utils/html';

export function errorPage() {
    return div(
        h1('Unexpected Error'),
        p(
            'An error occurred that is preventing this page from working. ',
            a({ onclick: 'javascript:location.reload()' }, 'Reload this page. '),
            a({ href: '/' }, 'Return home. '),
        )
    )
}