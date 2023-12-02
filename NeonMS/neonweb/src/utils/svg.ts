import { TagParam, createSvgElement } from './etc';

export function svg(...data: TagParam<SVGSVGElement>[]) {
    return createSvgElement('svg', ...data);
}

export function line(...data: TagParam<SVGLineElement>[]) {
    return createSvgElement('line', ...data);
}
