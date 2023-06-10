import { TagParams } from '../utils/etc';
import { button, div, form, h2, input, label } from '../utils/html';

export async function modalPrompt(text: string, title: string | undefined = undefined) {

    let data = '';

    const confirmed = await modalConfirm(
        title && h2(title),
        label(
            div(text),
            input({
                autofocus: true,
                onchange(ev: Event) { data = (ev.target as HTMLInputElement).value }
            })
        )
    );

    return confirmed ? data : undefined;
}

export function modalConfirm(...props: TagParams<HTMLFormElement>[]) {

    if (props.length === 1 && typeof props[0] === 'string') {
        props = [
            div(
                { onmount(ev: Event) { (ev.target as HTMLDivElement).closest('form')?.querySelector('button')?.focus() } },
                ...props
            )
        ]
    }

    return new Promise<boolean>((resolve) => {

        const view = div({ class: 'modal' },
            {
                onkeyup(ev: KeyboardEvent) {
                    if (ev.key === 'Escape') {
                        view.remove();
                        resolve(false);
                    }
                }
            },
            form({ class: 'modal-content card' },
                {
                    onsubmit(ev: SubmitEvent) {
                        ev.preventDefault();
                        view.remove();
                        resolve(true);
                    },
                    onkeyup(ev: KeyboardEvent) {
                        if (ev.ctrlKey && ev.key === 'Enter') {
                            (ev.currentTarget as HTMLFormElement).dispatchEvent(new Event('submit'));
                        }
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
}
