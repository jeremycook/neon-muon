import { Primitive } from '../database/database';
import { div } from '../utils/html';
import './spreadsheet.css';

export async function spreadsheet(records: (Primitive | null)[][]) {
    return div({ class: 'spreadsheet' },

        div({ class: 'spreadsheet-head' },
            div({ class: 'spreadsheet-corner' }),
            ...records[0]?.map((_, i) => [
                div({ class: 'spreadsheet-column-selector' },
                    String.fromCharCode(65 + i),
                    div({ class: 'spreadsheet-column-resizer' }, '')
                ),
            ])
        ),

        records.map((record) =>
            div({ class: 'spreadsheet-row' },
                div({ class: 'spreadsheet-row-selector' },
                    div({ class: 'spreadsheet-row-resizer' }, '')
                ),
                ...record.map(cell => [
                    div({ class: 'spreadsheet-cell' }, cell?.toString()),
                ])
            ),
        ),
    );
}
