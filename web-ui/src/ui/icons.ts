import { PubSubT } from '../utils/pubSub';
import { svg } from '../utils/svg';

const iconLookup = {
    'alert-12-regular': () => import('@iconify-icons/fluent/alert-12-regular'),
    'caret-right-12-regular': () => import('@iconify-icons/fluent/caret-right-12-regular'),
    'delete-12-regular': () => import('@iconify-icons/fluent/delete-12-regular'),
    'home-12-regular': () => import('@iconify-icons/fluent/home-12-regular'),
    'mail-read-16-regular': () => import('@iconify-icons/fluent/mail-read-16-regular'),
    'mail-unread-16-regular': () => import('@iconify-icons/fluent/mail-unread-16-regular'),
    'person-12-regular': () => import('@iconify-icons/fluent/person-12-regular'),
    'sparkle-16-regular': () => import('@iconify-icons/fluent/sparkle-16-regular'),
}

export type IconType = keyof typeof iconLookup;

// Iconify doesn't provide dimensions when for *-16-* icons
// so that must be the default.
const fallbackDim = 16;
const initialViewbox = '0 0 16 16';

export default function icon(type: IconType | PubSubT<IconType>) {
    const element = svg({ 'viewBox': initialViewbox, 'aria-hidden': '', class: 'icon' });

    if (typeof type === 'string') {
        // Fixed icon
        iconLookup[type]().then(module => {
            const data = module.default;
            element.setAttribute('viewBox', `0 0 ${data.width ?? fallbackDim} ${data.height ?? fallbackDim}`);
            element.innerHTML = data.body; // The contents will be filled as soon as it is available.
        });
    }
    else {
        // Dynamic icon
        const sub = () => {
            iconLookup[type.val]().then(module => {
                const data = module.default;
                element.setAttribute('viewBox', `0 0 ${data.width ?? fallbackDim} ${data.height ?? fallbackDim}`);
                element.innerHTML = data.body; // The contents will be filled as soon as it is available.
            });
        };
        const unsub = type.sub(sub);
        element.addEventListener('mount', () => sub());
        element.addEventListener('unmount', () => unsub());
    }

    return element;
}
