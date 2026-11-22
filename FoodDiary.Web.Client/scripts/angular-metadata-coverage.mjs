const signalQueryInitializers = new Map([
    ['ViewChild', 'viewChild'],
    ['ViewChildren', 'viewChildren'],
    ['ContentChild', 'contentChild'],
    ['ContentChildren', 'contentChildren'],
]);

function unwrap(node) {
    return node?.type === 'ParenthesizedExpression' ? unwrap(node.expression) : node;
}

function name(node) {
    if (node?.type === 'Identifier') {
        return node.name;
    }
    if (node?.type === 'Literal') {
        return node.value;
    }
}

function memberName(node) {
    if (node?.type === 'MemberExpression' && !node.computed) {
        return node.property.name;
    }
}

function unwrapViteReference(node) {
    const expression = unwrap(node);
    // Vite rewrites some imported references as `(0, importNamespace.member)`.
    if (
        expression?.type === 'SequenceExpression' &&
        expression.expressions.length === 2 &&
        expression.expressions[0].type === 'Literal' &&
        expression.expressions[0].value === 0
    ) {
        return unwrap(expression.expressions[1]);
    }
    return expression;
}

function propertyValue(node, propertyName) {
    if (node?.type === 'ObjectExpression') {
        return node.properties.find(property => property.type === 'Property' && !property.computed && name(property.key) === propertyName)
            ?.value;
    }
}

function isNgDevModeGuard(node) {
    const guard = unwrap(node);
    const test = guard?.left;
    return (
        guard?.type === 'LogicalExpression' &&
        guard.operator === '||' &&
        guard.right.type === 'Identifier' &&
        guard.right.name === 'ngDevMode' &&
        test?.type === 'BinaryExpression' &&
        test.operator === '===' &&
        test.left.type === 'UnaryExpression' &&
        test.left.operator === 'typeof' &&
        test.left.argument.type === 'Identifier' &&
        test.left.argument.name === 'ngDevMode' &&
        test.right.type === 'Literal' &&
        test.right.value === 'undefined'
    );
}

function classMetadataCall(statement) {
    const guard = unwrap(statement.expression);
    if (guard?.type !== 'LogicalExpression' || guard.operator !== '&&' || !isNgDevModeGuard(guard.left)) {
        return;
    }

    const call = unwrap(guard.right);
    if (call?.type !== 'CallExpression') {
        return;
    }
    if (memberName(call.callee) === 'ɵsetClassMetadata') {
        return { call };
    }
    if (memberName(call.callee) === 'ɵsetClassMetadataAsync' && call.arguments.length === 3) {
        const callback = call.arguments[2];
        const body = callback.type === 'ArrowFunctionExpression' && callback.body.type === 'BlockStatement' ? callback.body.body : [];
        const inner = body[0]?.expression;
        if (
            body.length === 1 &&
            body[0].type === 'ExpressionStatement' &&
            inner?.type === 'CallExpression' &&
            memberName(inner.callee) === 'ɵsetClassMetadata' &&
            name(inner.callee.object) === name(call.callee.object) &&
            name(inner.arguments[0]) === name(call.arguments[0])
        ) {
            return { call: inner, registration: body[0], loader: call.arguments[1], callback };
        }
    }
}

function zeroParameterArrow(node) {
    return node?.type === 'ArrowFunctionExpression' && !node.async && node.params.length === 0;
}

function staticText(node) {
    return (
        (node?.type === 'Literal' && typeof node.value === 'string') || (node?.type === 'TemplateLiteral' && node.expressions.length === 0)
    );
}

function sameImportedDependency(entry, binding) {
    if (entry?.type !== 'CallExpression' || memberName(entry.callee) !== 'then' || entry.arguments.length !== 1) {
        return false;
    }
    const imported = unwrap(entry.callee.object);
    const file = imported?.type === 'ImportExpression' && !imported.options ? imported.source : undefined;
    const viteFile =
        imported?.type === 'CallExpression' && name(imported.callee) === '__vite_ssr_dynamic_import__' && imported.arguments.length === 1
            ? imported.arguments[0]
            : undefined;
    const pick = entry.arguments[0];
    if (pick.type !== 'ArrowFunctionExpression' || pick.async || pick.params.length !== 1 || pick.params[0].type !== 'Identifier') {
        return false;
    }
    const exported = unwrap(pick.body);
    return (
        (file ?? viteFile)?.type === 'Literal' &&
        typeof (file ?? viteFile).value === 'string' &&
        exported?.type === 'MemberExpression' &&
        !exported.optional &&
        exported.object.type === 'Identifier' &&
        exported.object.name === pick.params[0].name &&
        memberName(exported) === binding.name
    );
}

function staticImportReference(node) {
    return (
        node?.type === 'Identifier' ||
        (memberName(node) && node.object.type === 'Identifier' && /^__vite_ssr_import_\d+__$/u.test(node.object.name))
    );
}

function hasStaticComponentMetadata(decorators, definition, bindings) {
    if (decorators.elements.length !== 1 || name(definition.key) !== 'ɵcmp') {
        return false;
    }
    const decorator = decorators.elements[0];
    if (
        decorator?.type !== 'ObjectExpression' ||
        decorator.properties.length !== 2 ||
        !decorator.properties.every(
            entry =>
                entry.type === 'Property' &&
                !entry.computed &&
                entry.kind === 'init' &&
                (name(entry.key) === 'type' || name(entry.key) === 'args'),
        )
    ) {
        return false;
    }
    const component = propertyValue(decorator, 'type');
    const args = propertyValue(decorator, 'args');
    const options = args?.type === 'ArrayExpression' && args.elements.length === 1 ? args.elements[0] : undefined;
    const declared = options?.type === 'ObjectExpression' ? options.properties : [];
    const keys = new Set(['selector', 'imports', 'changeDetection', 'template', 'styles']);
    if (
        declared.length !== keys.size ||
        !declared.every(entry => entry.type === 'Property' && !entry.computed && entry.kind === 'init' && keys.delete(name(entry.key)))
    ) {
        return false;
    }
    const selector = propertyValue(options, 'selector');
    const imported = propertyValue(options, 'imports');
    const detection = propertyValue(options, 'changeDetection');
    const styles = propertyValue(options, 'styles');
    const selectors = propertyValue(definition.value.arguments[0], 'selectors');
    const firstSelector = selectors?.type === 'ArrayExpression' && selectors.elements.length === 1 ? selectors.elements[0] : undefined;
    return (
        (memberName(component) ?? name(component)) === 'Component' &&
        selector?.type === 'Literal' &&
        typeof selector.value === 'string' &&
        firstSelector?.type === 'ArrayExpression' &&
        firstSelector.elements.length === 1 &&
        name(firstSelector.elements[0]) === selector.value &&
        imported?.type === 'ArrayExpression' &&
        imported.elements.every(staticImportReference) &&
        bindings.every(binding => imported.elements.some(entry => entry.type === 'Identifier' && entry.name === binding.name)) &&
        memberName(detection) === 'OnPush' &&
        (memberName(detection.object) ?? name(detection.object)) === 'ChangeDetectionStrategy' &&
        staticText(propertyValue(options, 'template')) &&
        styles?.type === 'ArrayExpression' &&
        styles.elements.every(staticText)
    );
}

function isSimpleAsyncMetadata(metadata, definition) {
    const { call, loader, callback } = metadata;
    const [, decorators, constructorMetadata, propertyDecorators] = call.arguments;
    if (
        !zeroParameterArrow(loader) ||
        loader.body.type !== 'ArrayExpression' ||
        callback.async ||
        constructorMetadata.type !== 'Literal' ||
        constructorMetadata.value !== null ||
        propertyDecorators.type !== 'Literal' ||
        propertyDecorators.value !== null
    ) {
        return false;
    }
    const bindings = callback.params;
    const imports = loader.body.elements;
    return (
        bindings.length > 0 &&
        bindings.length === imports.length &&
        bindings.every((binding, index) => binding.type === 'Identifier' && sameImportedDependency(imports[index], binding)) &&
        hasStaticComponentMetadata(decorators, definition, bindings)
    );
}

function reference(node) {
    const expression = unwrapViteReference(node);
    if (expression?.type === 'Identifier') {
        return expression.name;
    }
    if (memberName(expression)) {
        const owner = reference(expression.object);
        if (owner) {
            return `${owner}.${expression.property.name}`;
        }
    }
}

function queryInitializerName(node) {
    const callee = unwrapViteReference(node);
    if (memberName(callee) === 'required') {
        return queryInitializerName(callee.object);
    }
    return memberName(callee) ?? name(callee);
}

function findAngularDefinition(component, ownerName, receiver) {
    return component.body.body.find(field => {
        if (field.type !== 'PropertyDefinition' || field.computed || !field.static || field.value?.type !== 'CallExpression') {
            return false;
        }
        const helper = { ɵcmp: 'ɵɵdefineComponent', ɵdir: 'ɵɵdefineDirective' }[name(field.key)];
        const { callee, arguments: args } = field.value;
        const definedType = propertyValue(args[0], 'type');
        return (
            helper &&
            memberName(callee) === helper &&
            (receiver === undefined ? callee.object.type === 'Identifier' : name(callee.object) === receiver) &&
            definedType?.type === 'Identifier' &&
            (definedType.name === ownerName || definedType.name === component.id?.name)
        );
    });
}

function findNoDependencyFactory(component, ownerName, definedType) {
    const field = component.body.body.find(
        entry => entry.type === 'PropertyDefinition' && !entry.computed && entry.static && name(entry.key) === 'ɵfac',
    );
    const factory = unwrap(field?.value);
    if (
        factory?.type !== 'FunctionExpression' ||
        factory.async ||
        factory.generator ||
        factory.id?.name !== `${ownerName}_Factory` ||
        factory.params.length !== 1 ||
        factory.params[0].type !== 'Identifier' ||
        factory.params[0].name !== '__ngFactoryType__' ||
        factory.body.body.length !== 1
    ) {
        return;
    }
    const statement = factory.body.body[0];
    const result = statement.type === 'ReturnStatement' ? unwrap(statement.argument) : undefined;
    const target = result?.type === 'NewExpression' && result.arguments.length === 0 ? unwrap(result.callee) : undefined;
    if (
        target?.type === 'LogicalExpression' &&
        target.operator === '||' &&
        target.left.type === 'Identifier' &&
        target.left.name === '__ngFactoryType__' &&
        target.right.type === 'Identifier' &&
        target.right.name === definedType
    ) {
        return { statement, target };
    }
}

function templateCreationGate(metadata, ownerName) {
    const template = propertyValue(metadata, 'template');
    if (
        template?.type !== 'FunctionExpression' ||
        template.async ||
        template.generator ||
        template.id?.name !== `${ownerName}_Template` ||
        template.params.length !== 2 ||
        template.params[0].type !== 'Identifier' ||
        template.params[0].name !== 'rf' ||
        template.params[1].type !== 'Identifier' ||
        template.params[1].name !== 'ctx'
    ) {
        return;
    }
    const gate = template.body.body[0];
    const test =
        gate?.type === 'IfStatement' && gate.alternate === null && gate.consequent.type === 'BlockStatement' ? gate.test : undefined;
    if (
        test?.type === 'BinaryExpression' &&
        test.operator === '&' &&
        test.left.type === 'Identifier' &&
        test.left.name === 'rf' &&
        test.right.type === 'Literal' &&
        test.right.value === 1
    ) {
        return gate;
    }
}

function collectAngularDefinitionNodes(component, ownerName, branches, statements) {
    const definition = findAngularDefinition(component, ownerName);
    if (definition?.value.arguments.length !== 1) {
        return;
    }
    const metadata = definition.value.arguments[0];
    const factory = findNoDependencyFactory(component, ownerName, propertyValue(metadata, 'type').name);
    if (factory) {
        branches.add(factory.target);
        if (
            name(definition.key) === 'ɵdir' &&
            metadata.type === 'ObjectExpression' &&
            metadata.properties.every(entry => entry.type === 'Property' && !entry.computed && entry.kind === 'init') &&
            propertyValue(metadata, 'selectors') === undefined
        ) {
            // A directive without a selector cannot be instantiated by a template on its own.
            statements.add(factory.statement);
        }
    }
    if (name(definition.key) === 'ɵcmp') {
        const gate = templateCreationGate(metadata, ownerName);
        if (gate) {
            branches.add(gate);
        }
    }
}

function collectSignalQueryThunks(propertyDecorators, fields, angular, generated) {
    if (propertyDecorators.type !== 'ObjectExpression') {
        return;
    }

    for (const property of propertyDecorators.properties) {
        if (property.type !== 'Property' || property.computed) {
            continue;
        }
        const fieldName = name(property.key);
        const field = fields.find(entry => entry.type === 'PropertyDefinition' && !entry.computed && name(entry.key) === fieldName);
        const initializer = unwrap(field?.value);
        if (initializer?.type !== 'CallExpression' || property.value?.type !== 'ArrayExpression') {
            continue;
        }

        // An authored forwardRef stays a call in the original signal query initializer.
        const queryToken = reference(initializer.arguments[0]);
        if (!queryToken) {
            continue;
        }
        for (const decorator of property.value.elements) {
            const type = propertyValue(decorator, 'type');
            const query = signalQueryInitializers.get(memberName(type));
            if (!query || name(type.object) !== angular || queryInitializerName(initializer.callee) !== query) {
                continue;
            }
            const args = propertyValue(decorator, 'args');
            const locator = args?.type === 'ArrayExpression' ? args.elements[0] : undefined;
            if (
                locator?.type === 'CallExpression' &&
                locator.arguments.length === 1 &&
                memberName(locator.callee) === 'forwardRef' &&
                name(locator.callee.object) === angular &&
                zeroParameterArrow(locator.arguments[0]) &&
                reference(locator.arguments[0].body) === queryToken
            ) {
                generated.add(locator.arguments[0]);
            }
        }
    }
}

export function createAngularMetadataCoverageFilter() {
    const generated = new WeakSet();
    const generatedBranches = new WeakSet();
    const generatedStatements = new WeakSet();
    let mostRecentClass;

    return (node, type) => {
        // Vitest visits the enclosing statement before its arrows. Each marked arrow
        // contributes a function and a separate statement for its expression body.
        if (generated.has(node) && (type === 'function' || type === 'statement')) {
            return true;
        }
        if (type === 'branch' && generatedBranches.has(node)) {
            return true;
        }
        if (type !== 'statement') {
            return;
        }
        if (generatedStatements.has(node)) {
            return true;
        }

        if (mostRecentClass && node.start < mostRecentClass.start) {
            mostRecentClass = undefined;
        }
        if (node.type === 'VariableDeclarator' && node.init?.type === 'ClassExpression' && node.id.type === 'Identifier') {
            mostRecentClass = node;
            // Mark the compiler's own factory and control expressions, leaving template behavior in coverage.
            collectAngularDefinitionNodes(node.init, node.id.name, generatedBranches, generatedStatements);
        }
        if (node.type !== 'ExpressionStatement') {
            return;
        }

        const metadata = classMetadataCall(node);
        const call = metadata?.call;
        if (call?.arguments.length !== 4 || call.callee.object.type !== 'Identifier') {
            return;
        }
        const [owner, decorators, constructorMetadata, propertyDecorators] = call.arguments;
        if (owner.type !== 'Identifier' || decorators.type !== 'ArrayExpression') {
            return;
        }
        const candidate = mostRecentClass;
        mostRecentClass = undefined;
        const receiver = call.callee.object.name;
        if (candidate?.id.name !== owner.name || candidate.end >= node.start) {
            return;
        }
        const definition = findAngularDefinition(candidate.init, owner.name, receiver);
        if (!definition) {
            return;
        }
        // The async registration can repeat static decorator data. Leave nested paths and authored expressions in coverage.
        if (metadata.registration && isSimpleAsyncMetadata(metadata, definition)) {
            generatedStatements.add(metadata.registration);
            generatedStatements.add(metadata.loader);
            return true;
        }
        if (
            zeroParameterArrow(constructorMetadata) &&
            constructorMetadata.body.type === 'ArrayExpression' &&
            constructorMetadata.body.elements.length === 0
        ) {
            generated.add(constructorMetadata);
        }
        collectSignalQueryThunks(propertyDecorators, candidate.init.body.body, receiver, generated);
    };
}
