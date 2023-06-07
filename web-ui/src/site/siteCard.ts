import { div } from '../utils/html';

export function siteCard(...children: (string | Node)[]) {
    return div({ class: 'site-card' },
        div({ class: 'card' },
            ...children
        )
    )
}
