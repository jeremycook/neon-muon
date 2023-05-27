import { createDesigner } from './blueprint/blueprint-designer';
import { home } from './home/home';

export const routes: Record<string, ((params?: object) => (Node | Promise<Node>))> = {
    '/': home,
    '/designer': createDesigner,
}
