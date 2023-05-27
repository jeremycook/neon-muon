import { TagParams, createSvgElement } from './etc';

export function svg(...data: TagParams<SVGSVGElement>[]) {
    return createSvgElement('svg', ...data);
}

export function line(...data: TagParams<SVGLineElement>[]) {
    return createSvgElement('line', ...data);
}
