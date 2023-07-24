import { Primitive } from '../database/database';
import { EventT } from '../utils/etc';
import { div } from '../utils/html';
import './spreadsheet.css';

const vars = Object.freeze({
    defaultWidth: 96 as number,
    minWidth: 6 as number,
    dataTransferTypes: ['resize'],
});

export interface ColumnProp {
    label: string | null;
    width?: number;
}

export async function spreadsheet(
    columns: readonly ColumnProp[],
    records: (Primitive | null)[][]
) {
    const cols = columns.map((column, i) => ({
        label: column.label || String.fromCharCode(65 + i),
        width: Math.max(vars.minWidth, column.width ?? vars.defaultWidth),
    }));

    return div({ class: 'spreadsheet' }, {
        ondblclick: spreadsheet_ondblclick,
        ondrop: spreadsheet_ondrop,
    },
        div({ class: 'spreadsheet-head' }, {
            ondragover(ev: DragEvent) { ev.preventDefault(); },
        },
            div({ class: 'spreadsheet-corner' }),
            ...cols.map((column, i) => [
                div({ class: 'spreadsheet-column-selector' },
                    column.label,
                    div({ class: 'spreadsheet-column-resizer' }, {
                        draggable: true,
                        ondragstart: columnResizer_ondragstart(i),
                    }, '')
                ),
            ])
        ),

        records.map((record) =>
            div({ class: 'spreadsheet-row' },
                div({ class: 'spreadsheet-row-selector' },
                    div({ class: 'spreadsheet-row-resizer' }, '')
                ),
                ...record.map(cell => [
                    div({ class: 'spreadsheet-cell' },
                        div({ class: 'spreadsheet-content' },
                            cell?.toString()
                        )
                    ),
                ])
            ),
        ),
    );
}

function columnResizer_ondragstart(i: number) {
    return (ev: DragEvent) => {
        ev.dataTransfer!.setData('text/x-type', 'resize');
        ev.dataTransfer!.setData('text/x-column', i.toString());
        ev.dataTransfer!.setData('text/x-clientX', ev.clientX.toString());
    };
}

function spreadsheet_ondblclick(ev: EventT<HTMLDivElement>) {
    if (!(ev.target instanceof HTMLElement)) {
        return;
    }

    const spreadsheet = ev.currentTarget;
    const target = ev.target;

    if (target.matches('.spreadsheet-column-resizer')) {
        const selector = target.closest<HTMLDivElement>('.spreadsheet-column-selector')!;

        let index = 1;
        {
            let prev = selector.previousElementSibling;
            while (prev) {
                index++;
                prev = prev.previousElementSibling;
            }
        }

        let newWidth = vars.minWidth;

        const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-of-type(${index})`);
        for (const cell of cells) {
            const content = <HTMLElement>cell.firstElementChild;
            const width = content.scrollWidth;
            if (newWidth < width) {
                newWidth = width;
            }
        }

        const widthPx = newWidth + 'px';
        for (const cell of [selector, ...cells]) {
            cell.style.minWidth = widthPx;
            cell.style.maxWidth = widthPx;
        }

        ev.preventDefault();
    }
}

function spreadsheet_ondrop(ev: DragEvent & EventT<HTMLDivElement>) {
    const spreadsheet = ev.currentTarget;

    switch (ev.dataTransfer?.getData('text/x-type')) {
        case 'resize': {
            const column = parseInt(ev.dataTransfer!.getData('text/x-column'));
            const startX = parseInt(ev.dataTransfer!.getData('text/x-clientX'));
            const finalX = ev.clientX;
            const changeX = finalX - startX;

            const index = 2 + column;
            const selector = spreadsheet.querySelector<HTMLDivElement>(`.spreadsheet-column-selector:nth-of-type(${index})`)!;
            const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-of-type(${index})`);

            const newWidth = Math.max(vars.minWidth, selector.clientWidth + changeX);

            const widthPx = newWidth + 'px';
            for (const cell of [selector, ...cells]) {
                cell.style.minWidth = widthPx;
                cell.style.maxWidth = widthPx;
            }

            ev.preventDefault();
        }; break;

        default: break;
    }
}
