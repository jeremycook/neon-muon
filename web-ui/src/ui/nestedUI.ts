import { createFragment, createText } from '../utils/etc';
import { li, table, tbody, td, th, thead, tr, ul } from '../utils/html';

export function nestedUI(
    model: any
): Node {

    if (isEmpty(model)) {
        return createFragment();
    }

    if (Array.isArray(model)) {
        if (model.length > 0) {
            const keys = Object.keys(model[0]);
            if (typeof model[0] === 'object' && keys.every(key => typeof key === 'string')) {
                return nestedTableUI(keys, model)
            }
            else {
                return ul(...model.map(value =>
                    li(nestedUI(value))
                ));
            }
        }
        else {
            return createFragment();
        }
    }

    if (typeof model === 'object') {
        const entries = Object
            .entries(model as Record<string, any>)
            .filter(([, value]) => !isEmpty(value));
        if (entries.length > 0) {
            return ul(...entries.map(([key, value]: [string, any]) =>
                li(key, ': ', nestedUI(value))
            ));
        }
        else {
            return createFragment();
        }
    }

    return createText(model);
}

export function nestedListUI<TItem>(
    list: TItem[],
    renderer: (item: TItem) => Node | Node[]
): Node {

    if (isEmpty(list)) {
        return createFragment();
    }

    return ul(...list.map(value =>
        li(renderer(value))
    ));
}

export function nestedTableUI(
    columns: string[],
    model: Record<string, any>[],
    headerLabels?: string[]
): Node {
    return table(
        thead(
            tr(...columns.map((column, i) =>
                th(headerLabels?.[i] ?? column)
            ))
        ),
        tbody(...model.map(row =>
            tr(...columns.map(column =>
                td(nestedUI(row[column]))
            )))
        )
    );
}

function isEmpty(model: any) {
    return typeof model === 'undefined'
        || model === null
        || model === ''
        || (model?.length === 0);
}
