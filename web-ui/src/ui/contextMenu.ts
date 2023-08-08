import { EventT } from '../utils/etc';
import { dialog, div } from '../utils/html';

let contextMenuDialog: HTMLDialogElement | null = null;

export function contextMenu(
    ev: MouseEvent & Event & { currentTarget: HTMLElement; },
    ...contextMenuItems: HTMLElement[]
) {
    if (ev.altKey || ev.ctrlKey || ev.metaKey || ev.shiftKey) {
        return;
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
}
