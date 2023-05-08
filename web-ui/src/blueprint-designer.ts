import { h, svg } from "./etc.js";
import { ObservableSet } from "./observables.js";

type PortType = {
    category: string,
    kind: string,
    label: string,
    icon: string
}

type NodeTemplate = {
    type: string;
    label: string;
    ports: ({
        type: string;
        name: string;
        label?: string;
    })[];
};

type BlueprintPort = {
    type: string;
    name: string;
    label?: string;
};

type BlueprintNode = {
    id: string;
    type: string;
    ports: BlueprintPort[];
}

type BlueprintEdge = {
    sourceNodeId: string,
    sourcePortName: string,
    targetNodeId: string,
    targetPortName: string,
};

/**
 * Creates a blueprint designer HTMLDivElement.
 * @returns A blueprint designer HTMLDivElement.
 */
export function createDesigner() {

    const templates = {
        nodes: [
            {
                type: "ComparisonNode",
                label: "Comparison",
                ports: [
                    { type: "ActionPort", name: "Compare" },
                    { type: "InputPort", name: "Value1" },
                    { type: "InputPort", name: "Value2" },
                    { type: "EventPort", name: "Same" },
                    { type: "EventPort", name: "Different" },
                ]
            },
            {
                type: "HttpRequestNode",
                label: "HTTP Request",
                ports: [
                    { type: "InputPort", name: "PathConstraint", label: "Path Constraint" },
                    { type: "EventPort", name: "Started" },
                    { type: "EventPort", name: "Completed" },
                    { type: "OutputPort", name: "Path" },
                    { type: "OutputPort", name: "Method" },
                ]
            },
            {
                type: "HttpResponseNode",
                label: "HTTP Response",
                ports: [
                    { type: "ActionPort", name: "Respond" },
                    { type: "InputPort", name: "Body" },
                    { type: "EventPort", name: "Started" },
                    { type: "EventPort", name: "Completed" },
                ]
            },
        ],
        portTypes:
        {
            EventPort: { category: "Message", kind: "Source", label: "Event", icon: "▷" },
            ActionPort: { category: "Message", kind: "Target", label: "Action", icon: "▷" },

            OutputPort: { category: "Property", kind: "Source", label: "Output", icon: "○" },
            InputPort: { category: "Property", kind: "Target", label: "Input", icon: "○" },
        },
        edges:
        {

        }
    };

    const nodes = new Map<string, BlueprintNode>();
    const edges = new ObservableSet<BlueprintEdge>();

    function toggleTemplatePicker(ev: MouseEvent) {
        if (ev.currentTarget !== ev.target) {
            return;
        }

        if (!templatePicker.classList.contains("active")) {
            ev.preventDefault();

            templatePicker.style.left = (ev.offsetX) + "px";
            templatePicker.style.top = (ev.offsetY) + "px";
            templatePicker.classList.add("active");
        }
        else {
            templatePicker.classList.remove("active");
        }
    }

    function addNodeToCanvas(nodeTemplate: NodeTemplate) {
        const nodeData = {
            id: randomText(),
            type: nodeTemplate.type,
            label: nodeTemplate.label,
            ports: nodeTemplate.ports.map(port => Object.assign({}, port))
        };

        nodes.set(nodeData.id, nodeData);

        const node = h(".blueprint-node",
            {
                style: {
                    left: `${templatePicker.offsetLeft}px`,
                    top: `${templatePicker.offsetTop}px`,
                },
                "blueprint-node-id": nodeData.id,
            },
            h(".blueprint-node-header",
                { onmousedown: startDrag },
                nodeTemplate.label ?? nodeTemplate.type,
                h("button.blueprint-node-remove", "×", {
                    onclick: () => {
                        node.remove();
                        nodes.delete(nodeData.id);
                    }
                })
            ),
            nodeTemplate.ports.map(port => {
                const portType = <PortType>(<any>templates.portTypes)[port.type];

                return h(`.blueprint-port.blueprint-port-type-${port.type}.blueprint-port-category-${portType.category}.blueprint-port-kind-${portType.kind}`,

                    { "blueprint-port-name": port.name },

                    portType.kind == "Source" ?
                        [port.label ?? port.name, " ", h("button.blueprint-port-icon", portType.icon, { onclick: connectPorts })] :
                        [h("button.blueprint-port-icon", portType.icon, { onclick: connectPorts }), " ", port.label ?? port.name]
                )
            })
        );

        canvas.appendChild(node);
    }

    function randomText(): string {
        return (Number.MAX_SAFE_INTEGER * Math.random()).toFixed();
    }

    /**
     * Start dragging a node onmousedown.
     * @param ev
     */
    function startDrag(ev: PointerEvent) {
        ev.preventDefault();

        const handle = <HTMLElement>ev.currentTarget;
        const node = <HTMLElement>handle.closest(".blueprint-node");

        node.classList.add("is-moving");

        // The position of the mouse cursor will
        // be used to calculate the change in position
        // while dragging
        let x0 = ev.clientX;
        let y0 = ev.clientY;

        // Bind events
        designer.onmouseup = stopDrag;
        designer.onmousemove = dragging;

        function dragging(ev: MouseEvent) {
            ev.preventDefault();

            // Calculate the cursor's change in position
            const xDelta = ev.clientX - x0;
            const yDelta = ev.clientY - y0;

            // Update the initial position
            x0 = ev.clientX;
            y0 = ev.clientY;

            // Calculate the node's new position
            const newX = Math.min(designer.clientWidth - 10, Math.max(0, node.offsetLeft + xDelta));
            const newY = Math.min(designer.clientHeight - 10, Math.max(0, node.offsetTop + yDelta));

            // Set it
            node.style.left = newX + "px";
            node.style.top = newY + "px";
        }

        function stopDrag() {
            ev.preventDefault();

            // Cleanup
            node.classList.remove("is-moving");
            designer.onmouseup = null;
            designer.onmousemove = null;
        }
    };

    let connectionHolder: {
        node: HTMLElement,
        nodeId: string,
        port: HTMLElement,
        portName: string
    } | null = null;

    /**
     * Start or finish connected two ports onmouseclick.
     * @param ev
     */
    function connectPorts(ev: MouseEvent): void {
        const button = <HTMLButtonElement>ev.currentTarget;
        const port = <HTMLElement>button.closest(".blueprint-port");
        const node = <HTMLElement>port.closest(".blueprint-node");

        const nodeId = node.getAttribute("blueprint-node-id")!;
        const portName = port.getAttribute("blueprint-port-name")!;

        if (connectionHolder === null) {
            // Store the node and port info
            connectionHolder = {
                node: node,
                nodeId: nodeId,
                port: port,
                portName: portName,
            };
        }
        else {
            const edge: BlueprintEdge = {
                sourceNodeId: connectionHolder.nodeId,
                sourcePortName: connectionHolder.portName,
                targetNodeId: nodeId,
                targetPortName: portName
            };

            // Save the connection
            edges.add(edge);
            connectionHolder = null;

            // Draw the line
            drawEdge(edge);
        }
    }

    function drawEdge(edge: BlueprintEdge) {
        const { sourceNodeId, sourcePortName, targetNodeId, targetPortName } = edge;

        const sourceNode = findNode(sourceNodeId);
        const sourcePort = findPort(sourceNode, sourcePortName);
        const sourceOffset = calculatePortOffset(sourceNode, sourcePort, "Source");

        const targetNode = findNode(targetNodeId);
        const targetPort = findPort(targetNode, targetPortName);
        const targetOffset = calculatePortOffset(targetNode, targetPort, "Target");

        const connection = <SVGLineElement>svg("line.blueprint-connection", {
            onclick: _e => {
                connection.remove();
                edges.delete(edge);
            },
            x1: sourceOffset.left.toFixed(),
            y1: sourceOffset.top.toFixed(),
            x2: targetOffset.left.toFixed(),
            y2: targetOffset.top.toFixed(),
        });

        svgCanvas.appendChild(connection)

        sourceNode.addEventListener("mousemove", _e => {
            const offset = calculatePortOffset(sourceNode, sourcePort, "Source");

            connection.setAttribute("x1", offset.left.toFixed());
            connection.setAttribute("y1", offset.top.toFixed());
        })

        targetNode.addEventListener("mousemove", _e => {
            const offset = calculatePortOffset(targetNode, targetPort, "Target");

            connection.setAttribute("x2", offset.left.toFixed());
            connection.setAttribute("y2", offset.top.toFixed());
        })
    }

    function findNode(nodeId: string) {
        return <HTMLElement>canvas.querySelector(`[blueprint-node-id="${nodeId}"]`);
    }

    function findPort(node: HTMLElement, portName: string) {
        return <HTMLElement>node.querySelector(`[blueprint-port-name="${portName}"] button`);
    }

    function calculatePortOffset(node: HTMLElement, port: HTMLElement, kind: "Source" | "Target") {
        return {
            left: node.offsetLeft + (kind == "Source" ? node.clientWidth : 0),
            top: node.offsetTop + port.offsetTop + (port.clientHeight / 2)
        };
    }



    const templatePicker = <HTMLDivElement>h(".blueprint-template-picker",
        templates.nodes.map(node => h("button.blueprint-template-picker-node",
            {
                onclick: (_e: MouseEvent) => {
                    addNodeToCanvas(node);
                    templatePicker.classList.remove("active");
                }
            },
            h("div", node.label ?? node.type)
        ))
    );

    const svgCanvas = <SVGElement>svg("svg.blueprint-svg");

    const canvas = <HTMLDivElement>h(".blueprint-canvas", {
        onclick: toggleTemplatePicker
    });

    const designer = <HTMLDivElement>h(".blueprint-designer",
        canvas,
        h(".blueprint-svg-container", svgCanvas),
        templatePicker,
    );

    return designer;
}
