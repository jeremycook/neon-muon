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

    let lastSelectedCell: HTMLElement | null = null;

    return div({ class: 'spreadsheet' }, {
        ondragover(ev: DragEvent) { ev.preventDefault(); },
        ondrop: spreadsheet_ondrop,
    },
        div({ class: 'spreadsheet-head' }, {
        },
            div({ class: 'spreadsheet-corner' }),
            ...cols.map((column, i) => [
                div({ class: 'spreadsheet-column-selector' }, {
                    onpointerdown(ev: PointerEvent & EventT<HTMLDivElement>) {
                        if (ev.target.matches('.spreadsheet-column-resizer')) {
                            return; // Ignore clicks and double clicks on the resizer
                        }

                        const selector = ev.currentTarget;
                        const spreadsheet = selector.closest<HTMLDivElement>('.spreadsheet')!;
                        const domIndex = 2 + i;

                        if (!ev.shiftKey && !ev.ctrlKey) {
                            const selected = spreadsheet.querySelectorAll<HTMLElement>('.selected');
                            for (const element of selected) {
                                element.classList.remove('selected');
                            }
                        }

                        const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-of-type(${domIndex})`);
                        for (const element of [selector, ...cells]) {
                            element.classList.add('selected');
                        }

                        ev.preventDefault();
                    }
                },
                    column.label,
                    div({ class: 'spreadsheet-column-resizer' }, {
                        draggable: true,
                        ondragstart: columnResizer_ondragstart(i),
                        ondblclick: columnResizer_ondblclick,
                    }, '')
                ),
            ])
        ),

        records.map(record =>
            div({ class: 'spreadsheet-row' },
                div({ class: 'spreadsheet-row-selector' }, {
                    onpointerdown(ev: PointerEvent & EventT<HTMLDivElement>) {
                        const selector = ev.currentTarget;
                        const row = selector.closest<HTMLDivElement>('.spreadsheet-row')!;
                        const spreadsheet = row.closest<HTMLDivElement>('.spreadsheet')!;

                        if (!ev.ctrlKey) {
                            const selected = spreadsheet.querySelectorAll<HTMLElement>('.selected');
                            for (const element of selected) {
                                element.classList.remove('selected');
                            }
                        }

                        const cells = row.querySelectorAll<HTMLElement>('.spreadsheet-row-selector, .spreadsheet-cell');
                        if (ev.ctrlKey && row.querySelector(' .spreadsheet-row-selector.selected')) {
                            for (const element of cells) {
                                element.classList.remove('selected');
                            }
                        }
                        else {
                            for (const element of cells) {
                                element.classList.add('selected');
                            }
                        }

                        ev.preventDefault();
                    }
                },
                    div({ class: 'spreadsheet-row-resizer' }, '')
                ),
                ...record.map(cell =>
                    div({ class: 'spreadsheet-cell' }, {
                        onpointerdown(ev: PointerEvent & EventT<HTMLElement>) {
                            const cell = ev.currentTarget;
                            const spreadsheet = cell.closest<HTMLElement>('.spreadsheet')!;

                            if (ev.shiftKey) {

                                if (lastSelectedCell) {
                                    // Select a rectangle between there and here
                                    const lastSelectedRow = lastSelectedCell.closest<HTMLElement>('.spreadsheet-row')!;
                                    const lastSelectedRowIndex = getElementIndex(lastSelectedRow);
                                    const lastSelectedColumnIndex = getElementIndex(lastSelectedCell);

                                    const cellRow = cell.closest<HTMLElement>('.spreadsheet-row')!;
                                    const cellRowIndex = getElementIndex(cellRow);
                                    const cellColumnIndex = getElementIndex(cell);

                                    let currentRow = lastSelectedRowIndex < cellRowIndex ? lastSelectedRow : cellRow;
                                    const endRow = lastSelectedRowIndex > cellRowIndex ? lastSelectedRow : cellRow;

                                    const startIndex = lastSelectedColumnIndex < cellColumnIndex ? lastSelectedColumnIndex : cellColumnIndex;
                                    const endIndex = lastSelectedColumnIndex > cellColumnIndex ? lastSelectedColumnIndex : cellColumnIndex;

                                    while (true) {
                                        for (let index = startIndex; index <= endIndex; index++) {
                                            const child = <HTMLElement>currentRow.children[index];
                                            child.classList.add('selected');
                                        }
                                        if (currentRow === endRow) break;
                                        currentRow = <HTMLElement>currentRow.nextElementSibling;
                                    }
                                }
                                else {
                                    cell.classList.add('selected');
                                }
                                lastSelectedCell = cell;
                            }
                            else if (ev.ctrlKey) {
                                if (cell.matches('.selected')) {
                                    cell.classList.remove('selected');
                                }
                                else {
                                    cell.classList.add('selected');
                                    lastSelectedCell = cell;
                                }
                            }
                            else {
                                const selected = spreadsheet.querySelectorAll<HTMLElement>('.selected');
                                for (const element of selected) {
                                    element.classList.remove('selected');
                                }

                                cell.classList.add('selected');
                                lastSelectedCell = cell;
                            }

                            ev.preventDefault();
                        }
                    },
                        div({ class: 'spreadsheet-content' },
                            cell?.toString()
                        )
                    ),
                )
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

function columnResizer_ondblclick(ev: EventT<HTMLDivElement>) {
    const resizer = ev.currentTarget;
    const spreadsheet = resizer.closest<HTMLDivElement>('.spreadsheet')!;

    const selector = resizer.closest<HTMLDivElement>('.spreadsheet-column-selector')!;
    const domIndex = getDomColumnIndex(selector);
    const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-of-type(${domIndex})`);

    let newWidth = vars.minWidth;
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

function spreadsheet_ondrop(ev: DragEvent & EventT<HTMLDivElement>) {
    const spreadsheet = ev.currentTarget;

    switch (ev.dataTransfer?.getData('text/x-type')) {
        case 'resize': {
            const column = parseInt(ev.dataTransfer!.getData('text/x-column'));
            const startX = parseInt(ev.dataTransfer!.getData('text/x-clientX'));
            const finalX = ev.clientX;
            const changeX = finalX - startX;

            const domIndex = 2 + column;
            const selector = spreadsheet.querySelector<HTMLDivElement>(`.spreadsheet-column-selector:nth-of-type(${domIndex})`)!;
            const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-of-type(${domIndex})`);

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

function getDomColumnIndex(columnSelectorOrCell: HTMLElement) {
    let index = getElementIndex(columnSelectorOrCell);
    return index + 1;
}

function getElementIndex(columnSelectorOrCell: HTMLElement) {
    let index = 0;
    let prev = columnSelectorOrCell.previousElementSibling;
    while (prev) {
        index++;
        prev = prev.previousElementSibling;
    }
    return index;
}
