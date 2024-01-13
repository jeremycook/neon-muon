import { when } from "../utils/dynamicHtml";
import { ValueEvent, button, div, form, h1, hr, input, label, pre, textarea } from "../utils/html";
import { Val, val } from "../utils/pubSub";
import { QueryInput, QueryResult, previewBatch } from "./query";

export function queryPage() {

    const data: QueryInput = {
        server: undefined,
        database: '',
        actions: [
            {
                columns: true,
                sql: 'select *\nfrom db.meta'
            }
        ]
    };

    const errorMessage = val('');

    const queryResults = val<undefined | QueryResult[]>(undefined);

    const view = div({ class: 'flex flex-down gap' },
        h1('Ad-hoc Query'),

        ...when(errorMessage, () => div({ class: 'mb text-error' }, errorMessage.val)),

        form({
            class: 'flex flex-down gap',
            async onsubmit(ev: SubmitEvent) { await onsubmit(ev, errorMessage, data, queryResults); }
        },

            div({ class: 'flex gap' },
                label(
                    div('Server'),
                    input({ required: true, value: data.server, oninput(ev: ValueEvent) { data.server = ev.target.value } }),
                ),
                label(
                    div('Database'),
                    input({ required: true, value: data.database, oninput(ev: ValueEvent) { data.database = ev.target.value } }),
                ),
            ),
            label(
                div('SQL'),
                textarea({
                    required: true,
                    oninput(ev: ValueEvent) { data.actions[0].sql = ev.target.value }
                },
                    data.actions[0].sql
                ),
            ),
            div({ class: 'flex gap' },
                button({ type: 'submit' }, 'Preview'),
                button({ type: 'submit' }, 'Apply'),
            ),

        ),

        when(queryResults, () => div(

            hr(),

            pre(
                JSON.stringify(queryResults.val, undefined, 4)
            )

        ))
    );

    return view;
}

async function onsubmit(
    ev: SubmitEvent,
    errorMessage: Val<string>,
    queryInput: Readonly<QueryInput>,
    queryResults: Val<undefined | QueryResult[]>,
) {
    ev.preventDefault();

    var response = await previewBatch(queryInput);
    if (response.ok) {
        errorMessage.val = '';
        queryResults.val = response.result;
        return;
    }
    else {
        errorMessage.val = response.errorMessage || 'An error occured';
    }
}