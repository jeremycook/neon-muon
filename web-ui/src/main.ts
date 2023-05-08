import './style.css'
import { createDesigner } from './blueprint-designer.ts';

const app = document.getElementById('app')!;

const designer = createDesigner();

app.appendChild(designer);
