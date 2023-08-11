import { EventT, TagChild } from '../utils/etc';
import { button, dialog, div } from '../utils/html';
import { Partial } from '../utils/types';

let contextMenuDialog: HTMLDialogElement | null = null;

function shouldBlockContextMenu(ev: MouseEvent) {
    return ev.altKey || ev.ctrlKey || ev.metaKey || ev.shiftKey;
}

export function openContextMenu(
    ev: MouseEvent & EventT<Element>,
    ...contextMenuItems: TagChild[]
) {
    if (shouldBlockContextMenu(ev)) {
        return false;
    }

    ev.preventDefault();
    ev.stopPropagation();

    contextMenuDialog?.dispatchEvent(new Event('cancel'));

    contextMenuDialog = dialog({
        class: 'context-menu',
        style: {
            left: `${ev.pageX}px`,
            top: `${ev.pageY}px`
        },
        oncancel(ev: EventT<HTMLDialogElement>) {
            ev.currentTarget.remove();
        },
        onclose(ev: CloseEvent & EventT<HTMLDialogElement>) {
            ev.currentTarget.remove();
        },
        onkeydown(ev: KeyboardEvent) {
            if (ev.key === 'Escape') {
                ev.stopPropagation();
            }
        },
        onpointerdown(ev: PointerEvent) {
            ev.currentTarget?.dispatchEvent(new Event('cancel'));
        },
    },
        div({ class: 'context-menu-list' },
            ...contextMenuItems
        )
    );
    document.body.append(contextMenuDialog);
    contextMenuDialog.showModal();

    return true;
}

export function contextMenuItem(
    { icon, content, ...params }: {
        icon?: Element;
        content: Element | string;
    } & Partial<HTMLButtonElement>
) {
    return button({
        class: 'context-menu-item',
    },
        <HTMLButtonElement>params,
        div(icon),
        div(content),
        div(),
        div()
    );
}
