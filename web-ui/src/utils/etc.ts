export type EventT<TCurrentTarget = any, TTarget = any> = Event & { currentTarget: TCurrentTarget; target: TTarget };

export type TagParams<TElement> = (
    | undefined
    | null
    | false
    | string
    | Node
    | (string | Node)[]
    | { 'class': { [className: string]: boolean } }
    | { 'style': { [propertyName: string]: string } }
    | { [attributeName: string]: null | string | boolean | number | EventListener | Function }
    | TElement
);

/**
 * Create an HTML element.
 * @param tag The name of the tag.
 * @param data An array of strings, Nodes, and attribute objects.
 */
export function createHtmlElement<K extends keyof HTMLElementTagNameMap, TElement extends HTMLElementTagNameMap[K]>(tag: K, ...data: TagParams<TElement>[]) {
    return createElement<TElement>(tag, TagNamespace.html, ...data);
}

/**
 * Create an SVG element.
 * @param tag The name of the tag.
 * @param data An array of strings, Nodes, and attribute objects.
 */
export function createSvgElement<K extends keyof SVGElementTagNameMap, TElement extends SVGElementTagNameMap[K]>(tag: K, ...data: TagParams<TElement>[]) {
    return createElement<TElement>(tag, TagNamespace.svg, ...data);
}

export enum TagNamespace {
    html = "html",
    svg = "http://www.w3.org/2000/svg",
}

/**
 * Create an element with attributes and content.
 * @param tag The name of the tag.
 * @param namespace 
 * @param data An array of strings, Nodes, and attribute objects.
 */
export function createElement<TElement extends Element>(tag: string, namespace: TagNamespace, ...data: TagParams<TElement>[]): TElement {

    const element = namespace === TagNamespace.html
        ? document.createElement(tag)
        : document.createElementNS(namespace, tag);

    const newNodes: Node[] = [];
    for (let i = 0; i < data.length; i++) {
        const content = data[i];

        if (typeof content === 'string') {
            const child = createText(content);
            element.appendChild(child);
            newNodes.push(child);
        }
        else if (content instanceof Node) {
            element.appendChild(content);
            newNodes.push(content);
        }
        else if (content instanceof Array) {
            for (const node of content.flat(32)) {
                if (typeof node === "string") {
                    const child = createText(node);
                    element.appendChild(child);
                    newNodes.push(child);
                }
                else {
                    element.appendChild(node);
                    newNodes.push(node);
                }
            }
        }
        else if (typeof content === 'undefined' || content === null || content === false) {
            // Noop
        }
        else {
            for (const name in content) {
                const val = (<any>content)[name];
                const typeofVal = typeof val;
                if (name === "class" && typeofVal === 'object') {
                    Object.getOwnPropertyNames(val)
                        .forEach(prop => val[prop]
                            ? element.classList.add(...prop.split(' '))
                            : element.classList.remove(...prop.split(' ')));
                }
                else if (name === "style" && typeofVal === 'object') {
                    Object.getOwnPropertyNames(val)
                        .forEach(prop => element.style.setProperty(prop, val[prop]));
                }
                else {
                    if (typeofVal === "string" || val === true || typeofVal === 'number') {
                        element.setAttribute(name, val);

                        if (name === 'autofocus' && val === true) {
                            addMountEventListener(element, ev => {
                                if (_isInViewport(ev.currentTarget)) {
                                    ev.currentTarget.focus();
                                }
                            });
                        }
                        else if (name === 'autoselect' && val === true) {
                            addMountEventListener(element, ev => {
                                if (_isInViewport(ev.currentTarget)) {
                                    ev.currentTarget.focus();
                                    (ev.currentTarget as any).select();
                                }
                            });
                        }
                    }
                    else if (val === false || val === null) {
                        element.removeAttribute(name);
                    }
                    else if (typeofVal === "function" && name.startsWith('on')) {
                        switch (name) {
                            case 'onmount':
                                addMountEventListener(element, val);
                                break;
                            case 'onunmount':
                                addUnmountEventListener(element, val);
                                break;
                            default:
                                element.addEventListener(name.substring(2), val);
                                break;
                        }
                    }
                    else {
                        throw new Error(`The "${name}" attribute of type "${typeofVal}" is not supported. Event attributes must start with "on".`);
                    }
                }
            }
        }
    }

    return element as any;
}

/**
 * Create a text node.
 * @param content
 */
export function createText(content: string) {
    return document.createTextNode(content);
}

/**
 * Create a comment node.
 * @param content
 */
export function createComment(content: string) {
    return document.createComment(content);
}

/**
 * Create a document fragment.
 * @param content
 */
export function createFragment(...nodes: (Node | string)[]) {
    const fragment = document.createDocumentFragment();
    fragment.append(...nodes);
    return fragment;
}

/**
 * A series of nodes surrounded a begin and an end Comment.
 */
export type Segment = [HTMLTemplateElement, ...Node[], HTMLTemplateElement];

/**
 * Creates a series of nodes surrounded by begin and end template elements
 * that can be manipulated later.
 * @param nodes
 */
export function createSegment(...newNodes: Node[]): Segment {
    return [document.createElement('template'), ...newNodes, document.createElement('template')];
}

/**
 * Replace the nodes within the segment with new nodes.
 * @param segment 
 * @param newNodes 
 */
export function mutateSegment(segment: Segment, ...newNodes: (string | Node)[]) {
    const begin = segment[0];
    const end = segment[segment.length - 1] as HTMLTemplateElement;

    if (!(begin instanceof HTMLTemplateElement) || !(end instanceof HTMLTemplateElement)) {
        throw new Error('Segments must begin and end with template elements.');
    }

    let node = begin.nextSibling;
    while (node && node !== end) {
        const nextSibling = node.nextSibling;
        unmountElement(node);
        node = nextSibling;
    }

    // Add the new nodes and mutate the segment
    const addedNodes = newNodes.map(n => typeof n === 'string' ? createText(n) : n);
    begin.after(...addedNodes);
    segment.splice(1, segment.length - 2, ...addedNodes);

    // Notify new nodes that they have been mounted
    dispatchMountEvent(...addedNodes);
}

export function addMountEventListener<TCurrentTarget extends Element>(
    element: TCurrentTarget,
    listener: (this: TCurrentTarget, ev: EventT<TCurrentTarget>) => any,
    options?: boolean | AddEventListenerOptions | undefined
) {
    element.setAttribute('mountable', '');
    element.addEventListener('mount', listener as any, options);
}

export function addUnmountEventListener<TCurrentTarget extends Element>(
    element: TCurrentTarget,
    listener: (this: TCurrentTarget, ev: EventT<TCurrentTarget>) => any,
    options?: boolean | AddEventListenerOptions | undefined
) {
    element.setAttribute('mountable', '');
    element.addEventListener('unmount', listener as any, options);
}

export function dispatchMountEvent(...nodes: Node[]) {
    for (const ancestor of nodes) {
        if (ancestor instanceof Element) {
            if (ancestor.matches('[mountable]')) {
                ancestor.dispatchEvent(createMountEvent());
            }
            for (const descendant of ancestor.querySelectorAll('[mountable]')) {
                descendant.dispatchEvent(createMountEvent());
            }
        }
    }
}

export function dispatchUnmountEvent(...nodes: Node[]) {
    for (const ancestor of nodes) {
        if (ancestor instanceof Element) {
            if (ancestor.matches('[mountable]')) {
                ancestor.dispatchEvent(createUnmountEvent());
            }
            for (const descendant of ancestor.querySelectorAll('[mountable]')) {
                descendant.dispatchEvent(createUnmountEvent());
            }
        }
    }
}

/** Returns a new mount event. */
export function createMountEvent(): Event {
    return new Event('mount', { cancelable: false, bubbles: false });
}

export function createUnmountEvent(): Event {
    return new Event('unmount', { cancelable: false, bubbles: false });
}

export function mountElement(parent: Element, child: Node) {
    parent.append(child);
    dispatchMountEvent(child);
}

export function unmountElement(node: ChildNode) {
    dispatchUnmountEvent(node);
    node.remove();
}

function _isInViewport(element: Element) {
    const rect = element.getBoundingClientRect();
    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
}