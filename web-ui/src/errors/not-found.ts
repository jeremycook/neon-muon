import { a, div, h1, p } from '../utils/html';

export function notFound() {
    return div(
        h1('Page Not Found'),
        p(
            'The page you were looking for was not found. ',
            a({ href: '/' }, 'Return home.'),
        )
    )
}