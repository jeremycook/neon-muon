import { EventT, TagParam, mountElement, unmountElement } from '../utils/etc';
import { button, div, form, h2, input, label } from '../utils/html';

export async function modalPrompt(text: string, title: string = '', initialValue: string = '') {
    let data = '';

    const confirmed = await modalConfirm(
        title && h2(title),
        label(
            div(text),
            input({
                autofocus: true,
                value: initialValue,
                onchange(ev: Event) { data = (ev.target as HTMLInputElement).value }
            })
        )
    );

    return confirmed ? data : undefined;
}

export function modalConfirm(...props: TagParam<HTMLFormElement>[]) {
    return new Promise<boolean>((resolve) => {

        const view = div({ class: 'modal' }, {
            onkeyup(ev: KeyboardEvent) {
                if (ev.key === 'Escape') {
                    unmountElement(view);
                    resolve(false);
                }
            }
        },
            form({
                onsubmit(ev: SubmitEvent) {
                    ev.preventDefault();
                    unmountElement(view);
                    resolve(true);
                },
                onkeyup(ev: KeyboardEvent & EventT<HTMLFormElement>) {
                    if (ev.ctrlKey && ev.key === 'Enter') {
                        ev.currentTarget.dispatchEvent(new Event('submit'));
                    }
                }
            },
                ...props,

                div({ class: 'modal-footer' },
                    button({ type: 'submit' }, {
                        onmount(ev: EventT<HTMLButtonElement>) {
                            const form = ev.currentTarget.form!;
                            if ((document.activeElement as any)?.form === form) {
                                // Something in this form already has focus
                            }
                            else {
                                // Focus on the first input-ish element
                                (form.querySelector('button, input, select, textarea') as any)?.focus()
                            }
                        },
                    }, 'OK'),
                    button({
                        onclick() {
                            unmountElement(view);
                            resolve(false);
                        }
                    },
                        'Cancel'
                    )
                )
            )
        );

        mountElement(document.body, view);
    });
}
