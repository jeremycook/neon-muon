import { createFragment, createText } from '../utils/etc';
import { li, table, tbody, td, th, thead, tr, ul } from '../utils/html';

export function nestedList(model: any): Node {

    if (isEmpty(model)) {
        return createFragment();
    }

    if (Array.isArray(model)) {
        if (model.length > 0) {
            const keys = Object.keys(model[0]);
            if (typeof model[0] === 'object' && keys.every(key => typeof key === 'string')) {
                return table(
                    thead(
                        tr(
                            ...keys.map(k => th(k))
                        )
                    ),
                    tbody(
                        ...model.map(row => tr(
                            ...Object.values(row).map(cell => td(nestedList(cell)))
                        ))
                    )
                )
            }
            else {
                return ul(
                    ...model
                        .map(value => li(nestedList(value)))
                );
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
            return ul(
                ...entries
                    .map(([key, value]: [string, any]) => li(key, ': ', nestedList(value)))
            );
        }
        else {
            return createFragment();
        }
    }

    return createText(model);
}

function isEmpty(model: any) {
    return typeof model === 'undefined'
        || model === null
        || model === ''
        || (model?.length === 0);
}
