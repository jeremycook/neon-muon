import { siteCard } from '../site/siteCard';
import { a, h1, p } from '../utils/html';

export function notFoundPage() {
    return siteCard(
        h1('Page Not Found'),
        p(
            'The page you were looking for was not found. ',
            a({ href: '/' }, 'Return home.'),
        )
    )
}