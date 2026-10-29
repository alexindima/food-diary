/* eslint-disable @typescript-eslint/explicit-function-return-type, @typescript-eslint/explicit-member-accessibility -- JavaScript AST helpers use inferred shapes and cannot declare TypeScript access modifiers. */
const scrollMethods = new Set(['scroll', 'scrollTo', 'scrollBy', 'scrollIntoView', 'scrollToPosition', 'scrollToAnchor']);

function propertyName(node) {
    const key = node.property ?? node.key;
    return node.computed ? key?.value : (key?.name ?? key?.value);
}

function findVariable(context, node) {
    for (let scope = context.sourceCode.getScope(node); scope; scope = scope.upper) {
        const variable = scope.set.get(node.name);
        if (variable) {
            return variable;
        }
    }
    return;
}

function constantInitializer(context, node) {
    const definition = findVariable(context, node)?.defs[0];
    if (definition?.type === 'Variable' && definition.parent.kind === 'const') {
        return definition.node.init;
    }
    return;
}

class ScrollMotionInspector {
    constructor(context) {
        this.context = context;
        this.reported = new Set();
    }
    resolve(node, seen = new Set()) {
        if (!node || seen.has(node)) {
            return node;
        }
        seen.add(node);
        if (['ChainExpression', 'TSAsExpression', 'TSNonNullExpression', 'TSSatisfiesExpression'].includes(node.type)) {
            return this.resolve(node.expression, seen);
        }
        if (node.type !== 'Identifier') {
            return node;
        }
        const initializer = constantInitializer(this.context, node);
        return initializer ? this.resolve(initializer, seen) : node;
    }
    booleanDirection(node) {
        const right = this.resolve(node.right);
        if (typeof right?.value === 'boolean') {
            const invert = right.value === ['!==', '!='].includes(node.operator);
            return this.mediaDirection(node.left) * (invert ? -1 : 1);
        }
        const left = this.resolve(node.left);
        if (typeof left?.value === 'boolean') {
            const invert = left.value === ['!==', '!='].includes(node.operator);
            return this.mediaDirection(node.right) * (invert ? -1 : 1);
        }
        return 0;
    }
    queryDirection(node) {
        if (node.type !== 'MemberExpression' || propertyName(node) !== 'matches') {
            return 0;
        }
        const query = this.resolve(node.object);
        if (query?.type !== 'CallExpression') {
            return 0;
        }
        const callee = this.resolve(query.callee);
        if (propertyName(callee) !== 'matchMedia') {
            return 0;
        }
        const value = this.resolve(query.arguments[0])?.value;
        if (typeof value !== 'string') {
            return 0;
        }
        if (/^\(\s*prefers-reduced-motion\s*:\s*reduce\s*\)$/u.test(value)) {
            return 1;
        }
        return /^\(\s*prefers-reduced-motion\s*:\s*no-preference\s*\)$/u.test(value) ? -1 : 0;
    }
    mediaDirection(expression) {
        const node = this.resolve(expression);
        if (!node) {
            return 0;
        }
        if (node.type === 'UnaryExpression' && node.operator === '!') {
            return -this.mediaDirection(node.argument);
        }
        if (node.type === 'BinaryExpression' && ['===', '!==', '==', '!='].includes(node.operator)) {
            return this.booleanDirection(node);
        }
        if (node.type === 'LogicalExpression' && node.operator === '??' && this.resolve(node.right)?.value === false) {
            return this.mediaDirection(node.left);
        }
        return this.queryDirection(node);
    }
    writes(node) {
        const variable = findVariable(this.context, node);
        return variable
            ? variable.references.filter(reference => reference.isWrite() && reference.writeExpr).map(reference => reference.writeExpr)
            : [];
    }
    hasSmooth(expression, seen = new Set()) {
        const node = this.resolve(expression);
        if (!node || seen.has(node)) {
            return false;
        }
        seen.add(node);
        if (node.type === 'Literal') {
            return node.value === 'smooth';
        }
        if (node.type === 'TemplateLiteral') {
            return node.expressions.length === 0 && node.quasis[0].value.cooked === 'smooth';
        }
        return this.children(node).some(value => this.hasSmooth(value, seen));
    }
    children(node) {
        switch (node.type) {
            case 'Identifier': {
                return this.writes(node);
            }
            case 'ConditionalExpression': {
                return [node.consequent, node.alternate];
            }
            case 'LogicalExpression': {
                return [node.left, node.right];
            }
            default: {
                return [];
            }
        }
    }
    unsafe(expression) {
        const node = this.resolve(expression);
        if (node?.type !== 'ConditionalExpression') {
            return this.hasSmooth(node);
        }
        const direction = this.mediaDirection(node.test);
        if (direction === 1 && this.resolve(node.consequent)?.value === 'instant') {
            return false;
        }
        if (direction === -1 && this.resolve(node.alternate)?.value === 'instant') {
            return false;
        }
        return this.hasSmooth(node);
    }
    report(node) {
        if (this.reported.has(node)) {
            return;
        }
        this.reported.add(node);
        this.context.report({ node, messageId: 'unguarded' });
    }
    inspectProperty(property, seen) {
        if (property.type === 'SpreadElement') {
            this.inspectOptions(property.argument, seen);
        }
        if (property.type === 'Property' && propertyName(property) === 'behavior' && this.unsafe(property.value)) {
            this.report(property);
        }
    }
    inspectOptions(expression, seen = new Set()) {
        const node = this.resolve(expression);
        if (!node || seen.has(node)) {
            return;
        }
        seen.add(node);
        if (node.type === 'Identifier') {
            for (const value of this.writes(node)) {
                this.inspectOptions(value, seen);
            }
        }
        if (node.type === 'ObjectExpression') {
            for (const property of node.properties) {
                this.inspectProperty(property, seen);
            }
        }
    }
    inspectCall(node) {
        const callee = this.resolve(node.callee);
        if (!callee) {
            return;
        }
        const name = callee.type === 'Identifier' ? callee.name : propertyName(callee);
        if (scrollMethods.has(name)) {
            for (const argument of node.arguments) {
                this.inspectOptions(argument);
            }
        }
    }
}

export default {
    meta: {
        type: 'problem',
        docs: { description: 'Require explicit smooth scrolling to respect the reduced-motion media query.' },
        schema: [],
        messages: {
            unguarded:
                'Smooth scrolling must use the reduced-motion media query and an instant fallback. Reuse the shared scroll helper when possible.',
        },
    },
    create(context) {
        const inspector = new ScrollMotionInspector(context);
        return {
            CallExpression: node => inspector.inspectCall(node),
            AssignmentExpression(node) {
                if (node.left.type === 'MemberExpression' && propertyName(node.left) === 'behavior' && inspector.unsafe(node.right)) {
                    inspector.report(node);
                }
            },
        };
    },
};
