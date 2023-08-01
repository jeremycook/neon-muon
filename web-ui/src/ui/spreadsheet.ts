import { Primitive } from '../database/database';
import { EventT, dispatchMountEvent, dispatchUnmountEvent } from '../utils/etc';
import { div, input } from '../utils/html';
import { UnexpectedError } from '../utils/unreachable';
import './spreadsheet.css';

document.addEventListener('keydown', document_onkeydown);

let prevKey = 1;
const vars = Object.freeze({
    defaultWidth: 96 as number,
    minWidth: 6 as number,
    dataTransferTypes: ['resize'],
});

const datasheets: Record<string, DataSheet> = {};

// The last touched cell of any spreadsheet in the DOM
let activeCell: HTMLElement | null = null;
let activeEditor: HTMLElement | null = null;

export async function spreadsheet(props: {
    columns: readonly ColumnProp[];
    records: (Primitive | null)[][];
    onChange?: (change: SpreadsheetChange) => void,
}) {
    const key = (prevKey++).toString();
    const datasheet: DataSheet = {
        columns: props.columns.map((column, i) => ({
            label: column.label || String.fromCharCode(65 + i),
            width: Math.max(vars.minWidth, column.width ?? vars.defaultWidth),
            renderer: column.renderer ?? spreadsheetBasicContentRenderer,
            editor: column.editor ?? spreadsheetInputEditor,
        })),
        records: props.records,
        changes: new SpreadsheetChangeTracker(),
    };
    if (props.onChange) {
        datasheet.changes.addEventListener('ChangeValue', props.onChange);
    }
    datasheets[key] = datasheet;

    return div({
        'spreadsheet-key': key,
        class: 'spreadsheet',
        ondragover: spreadsheet_ondragover,
        ondrop: spreadsheet_ondrop,
        onmount: spreadsheet_onmount,
        onunmount: spreadsheet_onunmount,
    },
        div({ class: 'spreadsheet-head' },
            div({ class: 'spreadsheet-corner' }),
            ...datasheet.columns.map(column => [
                div({ class: 'spreadsheet-column-selector' }, {
                    onpointerdown: columnSelector_onpointerdown,
                },
                    column.label,
                    div({ class: 'spreadsheet-column-resizer' }, {
                        draggable: true,
                        ondragstart: columnResizer_ondragstart,
                        ondblclick: columnResizer_ondblclick,
                    })
                ),
            ])
        ),

        datasheet.records.map(record =>
            div({ class: 'spreadsheet-row' },
                div({ class: 'spreadsheet-row-selector' }, {
                    onpointerdown: rowSelector_onpointerdown,
                },
                    div({ class: 'spreadsheet-row-resizer' })
                ),
                ...record.map((value, i) =>
                    div({ class: 'spreadsheet-cell' }, {
                        ondblclick: cell_ondblclick,
                        onpointerdown: cell_onpointerdown,
                    },
                        datasheet.columns[i].renderer(value)
                    ),
                )
            ),
        ),
    );
}

export class ChangeValue {
    type = 'ChangeValue';
    constructor(
        public record: number,
        public column: number,
        public newValue: Primitive | null,
    ) { }
};

export type SpreadsheetChange =
    | ChangeValue;

export class SpreadsheetChangeTracker {
    #changes: SpreadsheetChange[] = [];

    #listeners: {
        Any: ((change: SpreadsheetChange) => void)[];
        ChangeValue: ((change: ChangeValue) => void)[];
    } = { Any: [], ChangeValue: [] };

    get length() {
        return this.#changes.length;
    }

    push(...items: SpreadsheetChange[]) {
        this.#changes.push(...items);
        for (const change of items) {
            for (const listener of (<any>this.#listeners)[change.type]) {
                listener(change);
            }
            for (const listener of this.#listeners.Any) {
                listener(change);
            }
        }
    }

    addEventListener(listener: (change: SpreadsheetChange) => void): void;
    addEventListener(type: 'ChangeValue', listener: (change: ChangeValue) => void): void;
    addEventListener(arg0: 'ChangeValue' | ((change: SpreadsheetChange) => void), arg1?: (change: ChangeValue) => void): void {
        if (typeof arg0 === 'string' && typeof arg1 === 'function') {
            this.#listeners[arg0].push(arg1);
        }
        else if (typeof arg0 === 'function') {
            this.#listeners['Any'].push(arg0);
        }
        else {
            throw new UnexpectedError(arg0, arg1);
        }
    }
}

export interface ColumnProp {
    label: string | null;
    width?: number;
    renderer?: (value: Primitive | null) => Node;
    editor?: (ev: Event, activeContent: HTMLElement) => HTMLElement;
}

interface DataColumn {
    label: string;
    width: number;
    renderer: (value: Primitive | null) => Node;
    editor: (ev: Event, activeContent: HTMLElement) => HTMLElement;
}

interface DataSheet {
    columns: DataColumn[];
    records: (Primitive | null)[][];
    changes: SpreadsheetChangeTracker;
}

export function spreadsheetBasicContentRenderer(value: Primitive | null) {
    return div({ class: 'spreadsheet-content' },
        value?.toString()
    );
}

export function spreadsheetInputEditor(ev: Event, activeContent: HTMLElement) {
    return input({
        class: 'spreadsheet-editor-content',
        value: ev instanceof KeyboardEvent && ev.key !== 'Enter' && ev.key !== 'F2'
            ? ''
            : activeContent.textContent,
        onkeydown: inputEditor_onkeydown
    });

    function inputEditor_onkeydown(ev: KeyboardEvent & EventT<HTMLInputElement>) {
        if (ev.key === 'Escape') {
            ev.currentTarget.parentNode!.dispatchEvent(new Event('cancel', { bubbles: true, cancelable: false }));
        }
        else if (ev.key === 'Enter' || ev.key === 'Tab') {
            const accept = new CustomEvent<AcceptEventDetails>('accept', {
                bubbles: true,
                cancelable: false,
                detail: {
                    original: ev,
                    value: ev.currentTarget.value,
                },
            });
            ev.currentTarget.parentNode!.dispatchEvent(accept);
        }
    }
}

let activeEditorEnterKeyCoolDown = Date.now() - 1;
function document_onkeydown(this: Document, ev: KeyboardEvent) {

    if (activeCell == null || activeEditor != null) {
        return;
    }

    const spreadsheet = activeCell.closest('.spreadsheet')!;

    if (ev.ctrlKey) {
        if (ev.key === 'a') {
            selectAllCells(spreadsheet);
            ev.preventDefault();
        }
        else if (ev.key === 'c') {
            copySelectedCells(spreadsheet);
            ev.preventDefault();
        }
        return;
    }
    else if (ev.altKey || ev.ctrlKey || ev.metaKey) {
        // TODO: Handle copy, paste, arrow keys, etc.
        return;
    }
    else if (ev.key === 'Escape') {
        deselectAll(spreadsheet);
        activeCell.classList.remove('active-cell');
        activeCell = null;
        return;
    }
    else if (ev.key === 'Tab') {
        const row = activeCell.closest('.spreadsheet-row')!;
        const cellColumnIndex = getElementIndex(activeCell);
        const columnDelta = ev.shiftKey
            ? Math.max(1, cellColumnIndex - 1)
            : Math.min(row.childElementCount - 1, cellColumnIndex + 1);

        deselectAll(spreadsheet);
        setActiveCell(<HTMLElement>row.childNodes[columnDelta]);

        ev.preventDefault();
        return;
    }
    else if (ev.key === 'ArrowLeft' || ev.key === 'ArrowRight') {
        const row = activeCell.closest('.spreadsheet-row')!;
        const cellColumnIndex = getElementIndex(activeCell);
        const columnDelta = ev.key === 'ArrowLeft'
            ? Math.max(1, cellColumnIndex - 1)
            : Math.min(row.childElementCount - 1, cellColumnIndex + 1);

        deselectAll(spreadsheet);
        setActiveCell(<HTMLElement>row.childNodes[columnDelta]);

        ev.preventDefault();
        return;
    }
    else if (ev.key === 'ArrowUp' || ev.key === 'ArrowDown') {
        const row = activeCell.closest<HTMLElement>('.spreadsheet-row')!;
        const cellRowIndex = getElementIndex(row);
        const cellColumnIndex = getElementIndex(activeCell);
        const rowDelta = ev.key === 'ArrowUp'
            ? Math.max(1, cellRowIndex - 1)
            : Math.min(spreadsheet.childElementCount - 1, cellRowIndex + 1);

        deselectAll(spreadsheet);
        setActiveCell(<HTMLElement>spreadsheet.childNodes[rowDelta].childNodes[cellColumnIndex]);

        ev.preventDefault();
        return;
    }

    if (ev.key.length === 1 || ev.key === 'F2') {
        // OK
    }
    else if (ev.key === 'Enter') {
        if (Date.now() < activeEditorEnterKeyCoolDown) {
            return;
        }
        else {
            // OK
        }
    }
    else {
        return;
    }

    activateEditor(ev);
}

function spreadsheet_ondragover(ev: DragEvent) {
    ev.preventDefault();
}

function spreadsheet_ondrop(ev: DragEvent & EventT<HTMLDivElement>) {
    const spreadsheet = ev.currentTarget;

    switch (ev.dataTransfer?.getData('text/x-type')) {
        case 'resize': {
            const columnPosition = parseInt(ev.dataTransfer!.getData('text/x-columnPosition'));
            const startX = parseInt(ev.dataTransfer!.getData('text/x-clientX'));
            const finalX = ev.clientX;
            const changeX = finalX - startX;

            const selector = spreadsheet.querySelector<HTMLDivElement>(`.spreadsheet-column-selector:nth-child(${columnPosition})`)!;
            const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-child(${columnPosition})`);

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

function spreadsheet_onmount(ev: EventT<HTMLDivElement>) {
    const spreadsheet = ev.currentTarget;
    const datasheet = getDataSheet(spreadsheet);

    datasheet.changes.addEventListener('ChangeValue', changeValue => {
        const row = spreadsheet.querySelector(`.spreadsheet-row:nth-child(${changeValue.record + 2})`);
        const cell = row?.querySelector(`.spreadsheet-cell:nth-child(${changeValue.column + 2})`);
        if (cell != null) {
            const column = datasheet.columns[changeValue.column];
            const content = column.renderer(changeValue.newValue);
            cell.replaceChildren(content);
            cell.classList.add('spreadsheet-cell-changed');
        }
    });
}

function spreadsheet_onunmount(ev: EventT<HTMLDivElement>) {
    const spreadsheet = ev.currentTarget;
    const key = getKey(spreadsheet);
    delete datasheets[key];
}

function columnSelector_onpointerdown(ev: PointerEvent & EventT<HTMLDivElement>) {
    if (ev.target.matches('.spreadsheet-column-resizer')) {
        return; // Ignore clicks and double clicks on the resizer
    }

    const selector = ev.currentTarget;
    const spreadsheet = selector.closest<HTMLDivElement>('.spreadsheet')!;
    const columnPosition = getElementPosition(selector);

    if (!ev.shiftKey && !ev.ctrlKey) {
        deselectAll(spreadsheet);
    }

    if (ev.ctrlKey && selector.matches('.selected-column')) {
        selector.classList.remove('selected-column');
        const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-child(${columnPosition})`);
        for (const cell of cells) {
            cell.classList.remove('selected-cell');
        }
        setActiveCell(spreadsheet.querySelector('.selected-cell'));
    }
    else if (ev.shiftKey && activeCell) {
        const lastActivePosition = getElementPosition(activeCell);
        const delta = lastActivePosition < columnPosition ? 1 : -1;
        for (let position = lastActivePosition; position != (columnPosition + delta); position += delta) {
            const column = spreadsheet.querySelector<HTMLElement>(`.spreadsheet-column-selector:nth-child(${position})`)!;
            const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-child(${position})`);

            column.classList.add('selected-column');
            for (const cell of cells) {
                cell.classList.add('selected-cell');
            }
            setActiveCell(cells.length > 0 ? cells.item(0) : spreadsheet.querySelector('.selected-cell'));
        }
    }
    else {
        selector.classList.add('selected-column');
        const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-child(${columnPosition})`);
        for (const cell of cells) {
            cell.classList.add('selected-cell');
        }
        setActiveCell(cells.length > 0 ? cells.item(0) : spreadsheet.querySelector('.selected-cell'));
    }

    ev.preventDefault();
}

function columnResizer_ondragstart(ev: DragEvent & EventT<HTMLElement>) {
    ev.dataTransfer!.setData('text/x-type', 'resize');
    ev.dataTransfer!.setData('text/x-columnPosition', getElementPosition(ev.currentTarget.closest('.spreadsheet-column-selector')!).toString());
    ev.dataTransfer!.setData('text/x-clientX', ev.clientX.toString());
}

function columnResizer_ondblclick(ev: EventT<HTMLDivElement>) {
    const resizer = ev.currentTarget;
    const spreadsheet = resizer.closest<HTMLDivElement>('.spreadsheet')!;

    const selector = resizer.closest<HTMLDivElement>('.spreadsheet-column-selector')!;
    const cellPosition = getElementPosition(selector);
    const cells = spreadsheet.querySelectorAll<HTMLElement>(`.spreadsheet-cell:nth-child(${cellPosition})`);

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

function rowSelector_onpointerdown(ev: PointerEvent & EventT<HTMLDivElement>) {
    const selector = ev.currentTarget;
    const row = selector.closest<HTMLDivElement>('.spreadsheet-row')!;
    const spreadsheet = row.closest<HTMLDivElement>('.spreadsheet')!;

    if (!ev.ctrlKey) {
        deselectAll(spreadsheet);
    }

    const cells = row.querySelectorAll<HTMLElement>('.spreadsheet-cell');
    if (ev.ctrlKey && row.querySelector('.selected-row') != null) {
        selector.classList.remove('selected-row');
        for (const element of cells) {
            element.classList.remove('selected-cell');
        }
        setActiveCell(spreadsheet.querySelector('.selected-cell'));
    }
    else {
        selector.classList.add('selected-row');
        for (const element of cells) {
            element.classList.add('selected-cell');
        }
        setActiveCell(cells.length > 0 ? cells.item(0) : spreadsheet.querySelector('.selected-cell'));
    }

    ev.preventDefault();
}

function cell_ondblclick(ev: MouseEvent & EventT<HTMLElement>) {
    activateEditor(ev);
}

function cell_onpointerdown(ev: PointerEvent & EventT<HTMLElement>) {
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
        deselectAll(spreadsheet);

        cell.classList.add('selected-cell');
        setActiveCell(cell);
    }

    ev.preventDefault();
}

interface AcceptEventDetails<TValue = any> {
    original: Event;
    value: TValue;
}

function activateEditor(ev: Event) {
    if (activeCell == null) {
        return;
    }

    const liveCell = activeCell;
    const spreadsheet = liveCell.closest('.spreadsheet')!;
    const selectedCells = spreadsheet.querySelectorAll<HTMLElement>('.selected-cell');

    const boundingRect = liveCell.getBoundingClientRect();
    const column = getDataColumn(liveCell);
    const contentEditor = column.editor(ev, liveCell);

    const newEditor = div({
        class: 'spreadsheet-editor',
        style: {
            'left': `${boundingRect.left}px`,
            'top': `${boundingRect.top}px`,
            'width': `${boundingRect.width}px`,
            'height': `${boundingRect.height}px`,
        },
        oncancel: function editor_oncancel(ev: Event) {
            ev.stopImmediatePropagation();

            // Discard
            unmountActiveEditor();
            liveCell.focus();
        },
        onaccept: function editor_onaccept(ev: CustomEvent<AcceptEventDetails> & EventT<Element>) {
            ev.stopImmediatePropagation();

            const sheet = getDataSheet(spreadsheet);

            // Apply changes to cells
            const newValue = ev.detail.value;
            for (const selectedContent of selectedCells) {
                const [record, column] = getDataCoordinates(selectedContent);
                sheet.changes.push({
                    type: "ChangeValue",
                    record,
                    column,
                    newValue,
                });
            }

            const original = ev.detail.original;
            if (original instanceof KeyboardEvent && original.key === 'Tab') {
                const cellColumnIndex = getElementIndex(liveCell);
                const row = liveCell.closest('.spreadsheet-row')!;
                const columnDelta = original.shiftKey
                    ? Math.max(1, cellColumnIndex - 1)
                    : Math.min(row.childElementCount - 1, cellColumnIndex + 1);

                deselectAll(spreadsheet);
                setActiveCell(<HTMLElement>row.childNodes[columnDelta]);
            }

            unmountActiveEditor();
            activeEditorEnterKeyCoolDown = Date.now() + 10;
            liveCell.focus();
        },
    },
        contentEditor
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

function copySelectedCells(spreadsheet: Element) {
    const datasheet = getDataSheet(spreadsheet);
    const cells = spreadsheet.querySelectorAll<HTMLElement>('.selected-cell');
    if (cells.length === 0) {
        return;
    }

    const [initialRecord, initialColumn] = getDataCoordinates(cells[0]);

    if (cells.length === 1) {
        const value = datasheet.records[initialRecord][initialColumn];
        navigator.clipboard.writeText(value?.toString() ?? '');
    }

    let minRecord: number | null = initialRecord;
    let maxRecord: number | null = initialRecord;
    let minColumn: number | null = initialColumn;
    let maxColumn: number | null = initialColumn;

    const coords: [number, number][] = [];
    for (const cell of cells) {
        const [record, column] = getDataCoordinates(cell);
        coords.push([record, column]);
        if (record < minRecord) minRecord = record;
        if (record > maxRecord) maxRecord = record;
        if (column < minColumn) minColumn = column;
        if (column > maxColumn) maxColumn = column;
    }

    const values: ((Primitive | null | undefined)[] | undefined)[] = [];
    for (const [record, column] of coords) {
        if (typeof values[record - minRecord] === 'undefined') {
            values[record - minRecord] = [];
        }
        values[record - minRecord]![column - minColumn] = datasheet.records[record][column];
    }

    const text = values.map(row => typeof row !== 'undefined'
        ? row.map(value => (value?.toString().replace('\t', '\\t') ?? '')).join('\t')
        : ''
    ).join('\n');
    navigator.clipboard.writeText(text);
}

function selectAllCells(spreadsheet: Element) {
    const cells = spreadsheet.querySelectorAll('.spreadsheet-cell');
    for (const element of cells) {
        element.classList.add('selected-cell');
    }
}

function deselectAll(spreadsheet: Element) {
    const cells = spreadsheet.querySelectorAll('.selected-cell');
    for (const element of cells) {
        element.classList.remove('selected-cell');
    }

    const columns = spreadsheet.querySelectorAll('.selected-column');
    for (const element of columns) {
        element.classList.remove('selected-column');
    }

    const rows = spreadsheet.querySelectorAll('.selected-row');
    for (const element of rows) {
        element.classList.remove('selected-row');
    }
}

function getSpreadsheet(element: Element) {
    return element.closest('.spreadsheet')!;
}

function getDataSheet(element: Element) {
    const spreadsheet = getSpreadsheet(element);
    const key = getKey(spreadsheet);
    const context = datasheets[key];
    return context;
}

function getKey(spreadsheet: Element) {
    return spreadsheet.getAttribute('spreadsheet-key')!;
}

/** 0-based index in a data record or the data columns. */
function getDataColumnIndex(columnSelectorOrCell: HTMLElement) {
    let index = getElementIndex(columnSelectorOrCell);
    return index - 1;
}

function getDataColumn(columnSelectorOrCell: HTMLElement) {
    const sheet = getDataSheet(columnSelectorOrCell);
    const index = getDataColumnIndex(columnSelectorOrCell);
    const field = sheet.columns[index];
    return field;
}

function getRow(cell: HTMLElement) {
    return cell.closest('.spreadsheet-row')!;
}

function getDataCoordinates(cell: HTMLElement) {
    const row = getRow(cell);
    const recordIndex = getElementIndex(row) - 1;
    const columnIndex = getElementIndex(cell) - 1;
    return [recordIndex, columnIndex];
}

/** 1-based position in DOM. */
function getElementPosition(element: Element) {
    let index = 1;
    let prev = element.previousElementSibling;
    while (prev) {
        index++;
        prev = prev.previousElementSibling;
    }
    return index;
}

/** 0-based index in DOM. */
function getElementIndex(element: Element) {
    let index = 0;
    let prev = element.previousElementSibling;
    while (prev) {
        index++;
        prev = prev.previousElementSibling;
    }
    return index;
}
