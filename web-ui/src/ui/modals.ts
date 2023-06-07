import { TagParams } from '../utils/etc';
import { button, div, form } from '../utils/html';

export function modalConfirm(...props: TagParams<HTMLFormElement>[]) {

    const promise = new Promise<boolean>((resolve) => {

        const view = div(
            {
                class: 'modal',
                onclick(ev: PointerEvent) {
                    if (ev.currentTarget === ev.target) {
                        view.remove();
                        resolve(false);
                    }
                },
                onkeyup(ev: KeyboardEvent) {
                    if (ev.key === 'Escape') {
                        view.remove();
                        resolve(false);
                    }
                }
            },
            form({
                class: 'modal-content card', onsubmit(ev: SubmitEvent) {
                    ev.preventDefault();
                    view.remove();
                    resolve(true);
                }
            },
                ...props,
                div({ class: 'flex gap-100' },
                    button('OK'),
                    button(
                        {
                            type: 'button',
                            onclick() {
                                view.remove();
                                resolve(false);
                            }
                        },
                        'Cancel'
                    )
                )
            )
        );

        document.body.append(view);
    });

    return promise;
}