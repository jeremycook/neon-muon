import { Primitive } from '../database/database';
import { EventT, dispatchMountEvent, dispatchUnmountEvent } from '../utils/etc';
import { div, input } from '../utils/html';
import './spreadsheet.css';

document.addEventListener('keydown', document_onkeydown);

const vars = Object.freeze({
    defaultWidth: 96 as number,
    minWidth: 6 as number,
    dataTransferTypes: ['resize'],
});

export interface ColumnProp {
    label: string | null;
    width?: number;
}

// The last touched cell of any spreadsheet in the DOM
let activeCell: HTMLElement | null = null;
let activeEditor: HTMLElement | null = null;

export async function spreadsheet(
    columns: readonly ColumnProp[],
    records: (Primitive | null)[][]
) {
    const cols = columns.map((column, i) => ({
        label: column.label || String.fromCharCode(65 + i),
        width: Math.max(vars.minWidth, column.width ?? vars.defaultWidth),
    }));

    return div({ class: 'spreadsheet' }, {
        ondragover(ev: DragEvent) {
            ev.preventDefault();
        },
        ondrop: spreadsheet_ondrop,
    },
        div({ class: 'spreadsheet-head' }, {
        },
            div({ class: 'spreadsheet-corner' }),
            ...cols.map(column => [
                div({ class: 'spreadsheet-column-selector' }, {
                    onpointerdown: columnSelector_onpointerdown,
                },
                    column.label,
                    div({ class: 'spreadsheet-column-resizer' }, {
                        draggable: true,
                        ondragstart: columnResizer_ondragstart,
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
                            const selected = spreadsheet.querySelectorAll<HTMLElement>('.selected-cell');
                            for (const element of selected) {
                                element.classList.remove('selected-cell');
                            }
                        }

                        const cells = row.querySelectorAll<HTMLElement>('.spreadsheet-cell');
                        if (ev.ctrlKey && row.querySelector(' .spreadsheet-row-selector.selected-row')) {
                            for (const element of cells) {
                                element.classList.remove('selected-cell');
                            }
                            setActiveCell(cells.length > 0 ? cells.item(0) : null);
                        }
                        else {
                            for (const element of cells) {
                                element.classList.add('selected-cell');
                            }
                            setActiveCell(cells.length > 0 ? cells.item(0) : null);
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

                                if (activeCell) {
                                    // Select a rectangle between there and here
                                    const activeCellRow = activeCell.closest<HTMLElement>('.spreadsheet-row')!;
                                    const activeCellRowIndex = getElementIndex(activeCellRow);
                                    const activeCellColumnIndex = getElementIndex(activeCell);

                                    const cellRow = cell.closest<HTMLElement>('.spreadsheet-row')!;
                                    const cellRowIndex = getElementIndex(cellRow);
                                    const cellColumnIndex = getElementIndex(cell);

                                    const rowDelta = activeCellRowIndex < cellRowIndex ? 1 : -1;
                                    const columnDelta = activeCellColumnIndex < cellColumnIndex ? 1 : -1;

                                    for (let rowIndex = activeCellRowIndex; rowIndex != (cellRowIndex + rowDelta); rowIndex += rowDelta) {
                                        const currentRow = spreadsheet.children[rowIndex];
                                        for (let colIndex = activeCellColumnIndex; colIndex != (cellColumnIndex + columnDelta); colIndex += columnDelta) {
                                            const currentCell = <HTMLElement>currentRow.children[colIndex];
                                            currentCell.classList.add('selected-cell');
                                        }
                                    }
                                }
                                else {
                                    cell.classList.add('selected-cell');
                                }

                                setActiveCell(cell);
                            }
                            else if (ev.ctrlKey) {
                                if (cell.matches('.selected-cell')) {
                                    cell.classList.remove('selected-cell');
                                }
                                else {
                                    cell.classList.add('selected-cell');
                                    setActiveCell(cell);
                                }
                            }
                            else {
                                const selected = spreadsheet.querySelectorAll<HTMLElement>('.selected-cell');
                                for (const element of selected) {
                                    element.classList.remove('selected-cell');
                                }

                                cell.classList.add('selected-cell');
                                setActiveCell(cell);
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

function setActiveCell(cell: (EventTarget & HTMLElement) | null) {
    activeCell?.classList.remove('active-cell');

    if (cell != null) {
        cell.classList.add('active-cell');
        cell.classList.add('selected-cell');
        if ((cell as any).scrollIntoViewIfNeeded) {
            (cell as any).scrollIntoViewIfNeeded();
        }
        else {
            cell.scrollIntoView({ block: 'center', inline: 'nearest' });
        }
    }

    activeCell = cell;
}

let activeEditorKeyCoolDown = Date.now();
function document_onkeydown(this: Document, ev: KeyboardEvent) {

    if (activeCell == null || activeEditor != null || Date.now() < activeEditorKeyCoolDown) {
        return;
    }

    if (ev.altKey || ev.ctrlKey || ev.metaKey) {
        // TODO: Handle copy, paste, arrow keys, etc.
        console.debug('Ignored')
        return;
    }

    if (ev.key === 'ArrowRight' || ev.key === 'Tab') {
        const spreadsheet = activeCell.closest('.spreadsheet')!;

        const selectedCells = spreadsheet.querySelectorAll('.selected-cell');
        for (const cell of selectedCells) {
            cell.classList.remove('selected-cell');
        }

        const cellColumnIndex = getElementIndex(activeCell);
        const row = activeCell.closest('.spreadsheet-row')!;

        if (cellColumnIndex < row.childElementCount - 1) {
            setActiveCell(<HTMLElement>row.childNodes[cellColumnIndex + 1]);
        }
        else {
            activeCell.classList.add('selected-cell');
        }

        ev.preventDefault();
        return;
    }
    else if (ev.key === 'ArrowLeft') {
        const spreadsheet = activeCell.closest('.spreadsheet')!;

        const selectedCells = spreadsheet.querySelectorAll('.selected-cell');
        for (const cell of selectedCells) {
            cell.classList.remove('selected-cell');
        }

        const cellColumnIndex = getElementIndex(activeCell);
        const row = activeCell.closest('.spreadsheet-row')!;

        if (cellColumnIndex > 1) {
            setActiveCell(<HTMLElement>row.childNodes[cellColumnIndex - 1]);
        }
        else {
            activeCell.classList.add('selected-cell');
        }

        ev.preventDefault();
        return;
    }
    else if (ev.key === 'ArrowUp') {
        const spreadsheet = activeCell.closest('.spreadsheet')!;
        const row = activeCell.closest<HTMLElement>('.spreadsheet-row')!;

        const selectedCells = spreadsheet.querySelectorAll('.selected-cell');
        for (const cell of selectedCells) {
            cell.classList.remove('selected-cell');
        }

        const cellColumnIndex = getElementIndex(activeCell);
        const cellRowIndex = getElementIndex(row);

        if (cellRowIndex > 1) {
            setActiveCell(<HTMLElement>spreadsheet.childNodes[cellRowIndex - 1].childNodes[cellColumnIndex]);
        }
        else {
            activeCell.classList.add('selected-cell');
        }

        ev.preventDefault();
        return;
    }
    else if (ev.key === 'ArrowDown') {
        const spreadsheet = activeCell.closest('.spreadsheet')!;
        const row = activeCell.closest<HTMLElement>('.spreadsheet-row')!;

        const selectedCells = spreadsheet.querySelectorAll('.selected-cell');
        for (const cell of selectedCells) {
            cell.classList.remove('selected-cell');
        }

        const cellColumnIndex = getElementIndex(activeCell);
        const cellRowIndex = getElementIndex(row);

        if (cellRowIndex < spreadsheet.childElementCount - 1) {
            setActiveCell(<HTMLElement>spreadsheet.childNodes[cellRowIndex + 1].childNodes[cellColumnIndex]);
        }
        else {
            activeCell.classList.add('selected-cell');
        }

        ev.preventDefault();
        return;
    }

    if (ev.key.length === 1) {
        // OK
    }
    else if (ev.key === 'Enter' || ev.key === 'F2') {
        // OK
    }
    else {
        console.debug('Ignored')
        return;
    }

    const ogContent = activeCell.querySelector('.spreadsheet-content')!;
    const cellContent = ogContent.textContent;

    const boundingRect = activeCell.getBoundingClientRect();
    const newEditor = div({
        class: 'spreadsheet-editor',
        style: {
            'left': `${boundingRect.left}px`,
            'top': `${boundingRect.top}px`,
            'width': `${boundingRect.width}px`,
            'height': `${boundingRect.height}px`,
        },
        onkeydown(ev: KeyboardEvent & EventT<HTMLInputElement>) {
            if (activeCell == null) {
                return;
            }

            if (ev.key === 'Escape') {
                unmountActiveEditor();

                activeCell.focus();

                ev.preventDefault();
                ev.stopImmediatePropagation();
            }
            else if (ev.key === 'Enter' || ev.key === 'Tab') {
                const spreadsheet = activeCell.closest('.spreadsheet')!;

                // Apply changes to cells
                const selected = spreadsheet.querySelectorAll('.selected-cell .spreadsheet-content');
                for (const selectedContent of selected) {
                    selectedContent.textContent = ev.currentTarget.querySelector<HTMLInputElement>('.spreadsheet-editor-input')!.value;
                }

                if (ev.key === 'Tab') {
                    // TODO: Move activeCell right or wrap if in last cell
                }

                unmountActiveEditor();
                activeEditorKeyCoolDown = Date.now() + 10;
                activeCell.focus();

                ev.preventDefault();
                ev.stopImmediatePropagation();
            }
        }
    },
        input({
            class: 'spreadsheet-editor-input',
            value: ev.key === 'Enter' || ev.key === 'F2'
                ? cellContent
                : '',
        })
    );

    unmountActiveEditor();

    activeEditor = newEditor;
    document.body.append(activeEditor);
    const firstInput = newEditor.querySelector<HTMLInputElement>('input:not([hidden])');
    if (firstInput != null) {
        firstInput.focus();
        firstInput.setSelectionRange(firstInput.value.length, firstInput.value.length);
    }
    dispatchMountEvent(newEditor);
}

function unmountActiveEditor() {
    if (activeEditor != null) {
        dispatchUnmountEvent(activeEditor as unknown as Node);
        (activeEditor as unknown as ChildNode).remove();
        activeEditor = null;
    }
}

function columnSelector_onpointerdown(ev: PointerEvent & EventT<HTMLDivElement>) {
    if (ev.target.matches('.spreadsheet-column-resizer')) {
        return; // Ignore clicks and double clicks on the resizer
    }

    const selector = ev.currentTarget;
    const spreadsheet = selector.closest<HTMLDivElement>('.spreadsheet')!;
    const columnPosition = getElementPosition(selector);

    if (!ev.shiftKey && !ev.ctrlKey) {
        const selected = spreadsheet.querySelectorAll<HTMLElement>('.selected-cell');
        for (const element of selected) {
            element.classList.remove('selected-cell');
        }
    }

    if (ev.ctrlKey && selector.matches('.selected-column')) {
        selector.classList.remove('selected-column');
        const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-row > :nth-child(${columnPosition})`);
        for (const cell of cells) {
            cell.classList.remove('selected-cell');
        }
        setActiveCell(spreadsheet.querySelector('.selected-cell'));
    }
    else if (ev.shiftKey && activeCell) {
        const lastActivePosition = getElementPosition(activeCell);
        const delta = lastActivePosition < columnPosition ? 1 : -1;
        for (let position = lastActivePosition; position != (columnPosition + delta); position += delta) {
            const column = spreadsheet.querySelector<HTMLElement>(`.spreadsheet-head > :nth-child(${position})`)!;
            const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-row > :nth-child(${position})`);

            column.classList.add('selected-column');
            for (const cell of cells) {
                cell.classList.add('selected-cell');
            }
            setActiveCell(cells.length > 0 ? cells.item(0) : spreadsheet.querySelector('.selected-cell'));
        }
    }
    else {
        selector.classList.add('selected-column');
        const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-row > :nth-child(${columnPosition})`);
        for (const cell of cells) {
            cell.classList.add('selected-cell');
        }
        setActiveCell(cells.length > 0 ? cells.item(0) : spreadsheet.querySelector('.selected-cell'));
    }

    ev.preventDefault();
}

function columnResizer_ondragstart(ev: DragEvent & EventT<HTMLElement>) {
    ev.dataTransfer!.setData('text/x-type', 'resize');
    ev.dataTransfer!.setData('text/x-column', getColumnIndex(ev.currentTarget.closest('.spreadsheet-cell')!).toString());
    ev.dataTransfer!.setData('text/x-clientX', ev.clientX.toString());
}

function columnResizer_ondblclick(ev: EventT<HTMLDivElement>) {
    const resizer = ev.currentTarget;
    const spreadsheet = resizer.closest<HTMLDivElement>('.spreadsheet')!;

    const selector = resizer.closest<HTMLDivElement>('.spreadsheet-column-selector')!;
    const domIndex = getElementPosition(selector);
    const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-child(${domIndex})`);

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
            const selector = spreadsheet.querySelector<HTMLDivElement>(`.spreadsheet-column-selector:nth-child(${domIndex})`)!;
            const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-child(${domIndex})`);

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

function getColumnIndex(columnSelectorOrCell: HTMLElement) {
    let index = getElementIndex(columnSelectorOrCell);
    return index - 1;
}

function getElementPosition(columnSelectorOrCell: HTMLElement) {
    let index = 1;
    let prev = columnSelectorOrCell.previousElementSibling;
    while (prev) {
        index++;
        prev = prev.previousElementSibling;
    }
    return index;
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
