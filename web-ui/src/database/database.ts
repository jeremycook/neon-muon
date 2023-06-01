import { nestedList } from '../ui/nestedList';
import { dynamic } from '../utils/dynamicHtml';
import { div, h1, h2 } from '../utils/html';
import { jsonGet } from '../utils/http';
import { val } from '../utils/pubSub';

type Database = {
    schemas: any[];
};

export async function database() {

    const database = val({ schemas: [] } as Database);

    const view = div(
        h1('Database'),
        dynamic(database, () => database.val.schemas.map(schema =>
            div(
                h2(schema.name ? schema.name : 'Main'),
                nestedList(schema)
            )
        ))
    );

    const response = await jsonGet<Database>('/api/database');
    if (response.result) {
        database.pub(response.result);
    }

    return view;
}
