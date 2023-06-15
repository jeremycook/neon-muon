import { SubT } from '../utils/pubSub';
import { svg } from '../utils/svg';

const iconLookup = {
    'alert-regular': () => import('@iconify-icons/fluent/alert-16-regular'),
    'caret-right-regular': () => import('@iconify-icons/fluent/caret-right-16-regular'),
    'delete-regular': () => import('@iconify-icons/fluent/delete-16-regular'),
    'home-regular': () => import('@iconify-icons/fluent/home-16-regular'),
    'mail-read-regular': () => import('@iconify-icons/fluent/mail-read-16-regular'),
    'mail-unread-regular': () => import('@iconify-icons/fluent/mail-unread-16-regular'),
    'person-regular': () => import('@iconify-icons/fluent/person-16-regular'),
    'sparkle-regular': () => import('@iconify-icons/fluent/sparkle-16-regular'),
}

export type IconType = keyof typeof iconLookup;

// Iconify doesn't provide dimensions for *-16-* icons so that must be the default.
const fallbackDim = 16;
const initialViewbox = '0 0 16 16';

export default function icon(type: IconType | SubT<IconType>) {
    const element = svg({ 'viewBox': initialViewbox, 'aria-hidden': '', class: 'icon' });

    if (typeof type === 'string') {
        // Static icon
        iconLookup[type]().then(module => {
            const data = module.default;
            element.setAttribute('viewBox', `0 0 ${data.width ?? fallbackDim} ${data.height ?? fallbackDim}`);
            element.innerHTML = data.body; // The contents will be filled as soon as it is available.
        });
    }
    else {
        // Dynamic icon
        const subscription = async () => {
            const module = await iconLookup[type.val]();
            const data = module.default;
            element.setAttribute('viewBox', `0 0 ${data.width ?? fallbackDim} ${data.height ?? fallbackDim}`);
            element.innerHTML = data.body; // The contents will be filled as soon as it is available.
        };
        type.sub(element, subscription);
        element.addEventListener('mount', subscription);
    }

    return element;
}
