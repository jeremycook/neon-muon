import { div, main } from '../utils/html';

export function mainLayout(...children: (string | Node)[]) {
    return main({ class: 'site-card' },
        div({ class: 'card' },
            ...children
        )
    )
}
