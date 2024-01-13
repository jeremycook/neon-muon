import { addMountEventListener } from '../utils/etc';
import { SubValue } from '../utils/pubSub';
import { svg } from '../utils/svg';

const iconLookup = {
    'alert-regular': () => import('@iconify-icons/fluent/alert-16-regular'),
    'arrow-download-regular': () => import('@iconify-icons/fluent/arrow-download-16-regular'),
    'arrow-upload-regular': () => import('@iconify-icons/fluent/arrow-upload-16-regular'),
    'caret-right-regular': () => import('@iconify-icons/fluent/caret-right-16-regular'),
    'clipboard-paste-filled': () => import('@iconify-icons/fluent/clipboard-paste-20-filled'),
    'copy-regular': () => import('@iconify-icons/fluent/copy-20-regular'),
    'delete-regular': () => import('@iconify-icons/fluent/delete-16-regular'),
    'document-edit-regular': () => import('@iconify-icons/fluent/document-edit-16-regular'),
    'document-save-regular': () => import('@iconify-icons/fluent/document-save-20-regular'),
    'folder-add-regular': () => import('@iconify-icons/fluent/folder-add-16-regular'),
    'home-regular': () => import('@iconify-icons/fluent/home-16-regular'),
    'mail-read-regular': () => import('@iconify-icons/fluent/mail-read-16-regular'),
    'mail-unread-regular': () => import('@iconify-icons/fluent/mail-unread-16-regular'),
    'person-regular': () => import('@iconify-icons/fluent/person-16-regular'),
    'plug-connected-add-regular': () => import('@iconify-icons/fluent/plug-connected-add-20-regular'),
    'rename-regular': () => import('@iconify-icons/fluent/rename-16-regular'),
    'sparkle-regular': () => import('@iconify-icons/fluent/sparkle-16-regular'),
    'table-delete-row-regular': () => import('@iconify-icons/fluent/table-delete-row-16-regular'),
    'table-insert-row-regular': () => import('@iconify-icons/fluent/table-insert-row-16-regular'),
    'text-add-regular': () => import('@iconify-icons/fluent/text-add-20-regular'),
}

export type IconType = keyof typeof iconLookup;

// Iconify doesn't provide dimensions for *-16-* icons so that must be the default.
const fallbackDim = 16;
const initialViewbox = '0 0 16 16';

/** Find more at: https://icon-sets.iconify.design/fluent/ */
export function icon(type: IconType | SubValue<IconType>) {
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
            const module = await iconLookup[type.value]();
            const data = module.default;
            element.setAttribute('viewBox', `0 0 ${data.width ?? fallbackDim} ${data.height ?? fallbackDim}`);
            element.innerHTML = data.body; // The contents will be filled as soon as it is available.
        };
        type.sub(element, subscription);
        addMountEventListener(element, subscription);
    }

    return element;
}
