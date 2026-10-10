import {
    existsSync,
    lstatSync,
    mkdirSync,
    readFileSync,
    readdirSync,
    realpathSync,
    unlinkSync,
    writeFileSync,
} from "node:fs";
import { dirname, isAbsolute, join, relative, resolve, sep } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

export const repositoryRoot = resolve(
    dirname(fileURLToPath(import.meta.url)),
    "../..",
);
const identifier = /^[A-Z][A-Za-z0-9]*$/u;
const featureName = /^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$/u;
const kebab = (value) =>
    value
        .replace(/([a-z0-9])([A-Z])/gu, "$1-$2")
        .replace(/([A-Z])([A-Z][a-z])/gu, "$1-$2")
        .toLowerCase();

export function containedPath(root, path) {
    const canonicalRoot = realpathSync(root);
    const target = resolve(canonicalRoot, path);
    const local = relative(canonicalRoot, target);
    if (
        !local ||
        isAbsolute(local) ||
        local.startsWith(`..${sep}`) ||
        local === ".." ||
        local.startsWith(sep)
    )
        throw new Error("Path must stay inside the owning workspace");
    const samePath = (left, right) =>
        process.platform === "win32"
            ? left.toLowerCase() === right.toLowerCase()
            : left === right;
    let parent = target;
    while (!existsSync(parent)) parent = dirname(parent);
    while (!samePath(parent, canonicalRoot)) {
        if (lstatSync(parent).isSymbolicLink())
            throw new Error("Generated paths must not traverse symbolic links");
        const next = dirname(parent);
        if (samePath(next, parent))
            throw new Error("Path must stay inside the owning workspace");
        parent = next;
    }
    return target;
}

function backendPlan(root, options) {
    const { module, name, kind = "query", response = "int" } = options;
    if (
        !identifier.test(module ?? "") ||
        !identifier.test(name ?? "") ||
        !["command", "query"].includes(kind)
    )
        throw new Error(
            "Use a known module, PascalCase name and command/query kind",
        );
    if (
        !/^(?:int|long|bool|string|Guid|[A-Z][A-Za-z0-9]*(?:\.[A-Z][A-Za-z0-9]*)+)$/u.test(
            response,
        )
    )
        throw new Error(
            "Response must be a scalar or fully qualified existing type",
        );
    const owner = `Modules/${module}/Application`;
    const ownerPath = containedPath(root, owner);
    if (
        !existsSync(ownerPath) ||
        !readdirSync(ownerPath).some((path) => path.endsWith(".csproj"))
    )
        throw new Error("The owning Application project must already exist");
    const projectName = readdirSync(ownerPath).find((path) =>
        path.endsWith(".csproj"),
    );
    const project = readFileSync(join(ownerPath, projectName), "utf8");
    const rootNamespace =
        project.match(/<RootNamespace>([^<]+)<\/RootNamespace>/u)?.[1] ??
        projectName.slice(0, -7);
    const suffix = kind === "command" ? "Command" : "Query";
    const slice = name.endsWith(suffix) ? name.slice(0, -suffix.length) : name;
    const type = `${slice}${suffix}`;
    const namespace = `${rootNamespace}.${suffix === "Command" ? "Commands" : "Queries"}.${slice}`;
    const prefix = `${owner}/${suffix === "Command" ? "Commands" : "Queries"}/${slice}`;
    const request = `using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;\nusing FoodDiary.Results;\n\nnamespace ${namespace};\n\npublic sealed record ${type}(Guid? UserId) : I${suffix}<Result<${response}>>, IUserRequest;\n`;
    const handler = `using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;\nusing FoodDiary.Results;\n\nnamespace ${namespace};\n\npublic sealed class ${type}Handler : I${suffix}Handler<${type}, Result<${response}>> {\n    public Task<Result<${response}>> Handle(${type} ${kind}, CancellationToken cancellationToken) {\n        throw new NotSupportedException("Resolve current-user access and implement owner capabilities before publishing this slice.");\n    }\n}\n`;
    const validator = `using FluentValidation;\n\nnamespace ${namespace};\n\npublic sealed class ${type}Validator : AbstractValidator<${type}> {\n    public ${type}Validator() {\n        RuleFor(request => request.UserId)\n            .NotNull().WithErrorCode("Validation.Required")\n            .Must(value => value is not null && value.Value != Guid.Empty).WithErrorCode("Validation.Invalid");\n    }\n}\n`;
    return {
        owner,
        files: [
            { path: `${prefix}/${type}.cs`, content: request },
            { path: `${prefix}/${type}Handler.cs`, content: handler },
            { path: `${prefix}/${type}Validator.cs`, content: validator },
        ],
        obligations: [
            "Implement authorization, owning capabilities and meaningful behavior checks.",
            "Review transaction semantics before registering a command.",
            "Run owner tests and architecture checks. No project references or DI registration are generated.",
        ],
    };
}

function frontendPlan(root, options) {
    const { feature, name, sdk, operation, api = sdk } = options;
    if (
        !featureName.test(feature ?? "") ||
        !identifier.test(name ?? "") ||
        !featureName.test(sdk ?? "") ||
        !/^[a-z][A-Za-z0-9]*$/u.test(operation ?? "") ||
        !/^[a-z][A-Za-z0-9]*$/u.test(api ?? "")
    )
        throw new Error(
            "Use an owning feature, PascalCase name and existing SDK operation",
        );
    const owner = `FoodDiary.Web.Client/src/app/features/${feature}`;
    if (!existsSync(containedPath(root, owner)))
        throw new Error("The owning feature must already exist");
    const source = readFileSync(
        containedPath(
            root,
            `FoodDiary.Web.Client/src/app/shared/api/sdk/generated/api/${sdk}.service.ts`,
        ),
        "utf8",
    );
    const sdkType = source.match(
        /export class (\w+) extends BaseService/u,
    )?.[1];
    const signature = source.match(
        new RegExp(
            `public ${operation}\\(\\s*requestParameters: (\\w+),[\\s\\S]*?\\): Observable<([^;]+?)>;`,
            "u",
        ),
    );
    if (!sdkType || !signature)
        throw new Error(
            "SDK operation is absent from the current generated client",
        );
    const [, paramsType, responseType] = signature;
    const dtoNames = [
        ...new Set(responseType.match(/[A-Z][A-Za-z0-9]+/gu) ?? []),
    ].filter((type) => type !== "Array");
    const keySet = readFileSync(
        containedPath(
            root,
            existsSync(
                join(
                    root,
                    "FoodDiary.Web.Client/src/environments/environment.development.ts",
                ),
            )
                ? "FoodDiary.Web.Client/src/environments/environment.development.ts"
                : "FoodDiary.Web.Client/src/environments/environment.ts",
        ),
        "utf8",
    );
    if (!new RegExp(`\\b${api}:`, "u").test(keySet))
        throw new Error(
            "API URL key is absent from the current application configuration",
        );
    const stem = kebab(name);
    const imports = dtoNames
        .map(
            (type) =>
                `import type { ${type} } from '../../../shared/api/sdk/generated/model/${kebab(type)}';`,
        )
        .join("\n");
    const adapter = `import { HttpClient } from '@angular/common/http';\nimport { inject, Service } from '@angular/core';\nimport type { Observable } from 'rxjs';\n\nimport { environment } from '../../../../environments/environment';\nimport { ${sdkType}, type ${paramsType} } from '../../../shared/api/sdk/generated/api/${sdk}.service';\n${imports}\nimport { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';\n\nexport type ${name}Request = Omit<${paramsType}, 'version'>;\n\n@Service()\nexport class ${name}Api {\n    private readonly sdk = createSdkConnection(${sdkType}, environment.apiUrls.${api}, inject(HttpClient));\n\n    public execute(request: ${name}Request): Observable<${responseType}> {\n        return this.sdk.client.${operation}({ ...request, version: this.sdk.version });\n    }\n}\n`;
    const facade = `import { DestroyRef, inject, Injectable } from '@angular/core';\nimport { takeUntilDestroyed } from '@angular/core/rxjs-interop';\nimport type { Subscription } from 'rxjs';\n\n${imports}\nimport { RequestStateController } from '../../../shared/lib/request-state';\nimport { ${name}Api, type ${name}Request } from '../api/${stem}.api';\n\n/** Provide this facade at its route/page boundary. Decode wire data in the owning adapter. */\n@Injectable()\nexport class ${name}Facade {\n    private readonly api = inject(${name}Api);\n    private readonly destroyRef = inject(DestroyRef);\n    private readonly request = new RequestStateController<${responseType}, unknown>();\n    private active: Subscription | undefined;\n\n    public readonly state = this.request.state;\n\n    public load(parameters: ${name}Request): void {\n        this.active?.unsubscribe();\n        const version = this.request.begin();\n        this.active = this.api.execute(parameters).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({\n            next: value => this.request.succeed(version, value),\n            error: (error: unknown) => this.request.fail(version, error),\n        });\n    }\n}\n`;
    return {
        owner,
        files: [
            { path: `${owner}/api/${stem}.api.ts`, content: adapter },
            { path: `${owner}/lib/${stem}.facade.ts`, content: facade },
        ],
        obligations: [
            "Provide the facade at its route/page boundary.",
            "Decode wire DTOs and preserve semantic IDs, quantities and dates in the owning adapter.",
            "Add behavior assertions for cancellation and error recovery before using this feature.",
            "No routes, cross-feature capabilities or DI bindings are registered automatically.",
        ],
    };
}

export function featurePlan(options, root = repositoryRoot) {
    const plan =
        options.target === "backend"
            ? backendPlan(root, options)
            : options.target === "frontend"
              ? frontendPlan(root, options)
              : (() => {
                    throw new Error("Target must be backend or frontend");
                })();
    for (const file of plan.files) {
        if (existsSync(containedPath(root, file.path)))
            throw new Error(`Refusing to overwrite ${file.path}`);
    }
    return {
        schemaVersion: 1,
        target: options.target,
        ...plan,
        readyForProduction: false,
    };
}

export function applyFeaturePlan(plan, root = repositoryRoot) {
    const paths = plan.files.map((file) => containedPath(root, file.path));
    for (const path of paths)
        if (existsSync(path))
            throw new Error(`Refusing to overwrite ${relative(root, path)}`);
    const created = [];
    try {
        plan.files.forEach((file, index) => {
            mkdirSync(dirname(paths[index]), { recursive: true });
            writeFileSync(paths[index], file.content, {
                encoding: "utf8",
                flag: "wx",
            });
            created.push(paths[index]);
        });
    } catch (error) {
        for (const path of created) unlinkSync(path);
        throw error;
    }
    return paths.map((path) => relative(root, path).replaceAll("\\", "/"));
}

export function parseOptions(args) {
    const options = { target: args[0] };
    for (let index = 1; index < args.length; index++) {
        const key = args[index].replace(/^--/u, "");
        if (
            ![
                "module",
                "name",
                "kind",
                "response",
                "feature",
                "sdk",
                "operation",
                "api",
                "apply",
                "dry-run",
            ].includes(key)
        )
            throw new Error(`Unknown generator option: ${args[index]}`);
        if (key === "apply" || key === "dry-run") options[key] = true;
        else {
            const value = args[++index];
            if (!value || value.startsWith("--"))
                throw new Error(`Missing generator value: ${key}`);
            options[key] = value;
        }
    }
    if (options.apply && options["dry-run"])
        throw new Error("Choose apply or dry-run");
    return options;
}

if (
    process.argv[1] &&
    pathToFileURL(resolve(process.argv[1])).href === import.meta.url
) {
    try {
        const options = parseOptions(process.argv.slice(2));
        const plan = featurePlan(options);
        console.log(
            JSON.stringify(
                {
                    ...plan,
                    applied: options.apply ? applyFeaturePlan(plan) : [],
                },
                null,
                2,
            ),
        );
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
