using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

bool buildBackendIndex = args is ["--backend-index"];
if (!buildBackendIndex && args is not ["--stdin"]) {
    Console.Error.WriteLine("Pass --stdin for reference counts or --backend-index for the canonical compiled index.");
    return 2;
}

var serializerOptions = new JsonSerializerOptions {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
// Console.In's own StreamReader falls back to a platform-default codepage when stdin is
// redirected from a file with no byte-order mark to auto-detect (as here, since the caller
// deliberately writes stdin BOM-free), which corrupts non-ASCII names; reading the raw
// stream with an explicit UTF-8 decoder avoids that regardless of platform or redirection.
using var stdinReader = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: false);
ScannerInput? input = JsonSerializer.Deserialize<ScannerInput>(stdinReader.ReadToEnd(), serializerOptions);
string[] names = input?.Names ?? input?.Contracts?.Select(contract => contract.Name).ToArray() ?? [];
if (input is null || input.Paths.Length == 0 || names.Length == 0) {
    Console.Error.WriteLine("The scanner payload must contain paths and either names or contracts.");
    return 2;
}

var scanner = new MultiPatternScanner(names);
if (buildBackendIndex) {
    if (input.Contracts is null || input.Contracts.Length == 0) {
        Console.Error.WriteLine("Backend-index mode requires contract metadata.");
        return 2;
    }
    BackendIndex index = BuildBackendIndex(input.Paths, input.Contracts, scanner);
    Console.Error.WriteLine($"LLM_WIKI_METRICS contracts={index.Summary.Contracts};consumerEdges={index.Summary.ConsumerEdges}");
    Console.Write(CanonicalJson(index, serializerOptions));
    return 0;
}

var results = new List<FileReferenceResult>(input.Paths.Length);
foreach (string path in input.Paths.Distinct(StringComparer.Ordinal)) {
    string content = File.ReadAllText(path);
    IReadOnlyList<ReferenceCount> references = scanner.Count(content);
    results.Add(new FileReferenceResult(path.Replace('\\', '/'), references));
}

Console.WriteLine(JsonSerializer.Serialize(results, serializerOptions));
return 0;

static BackendIndex BuildBackendIndex(string[] paths, ContractInput[] contracts, MultiPatternScanner scanner) {
    Dictionary<string, ContractInput> contractsByName = contracts.ToDictionary(contract => contract.Name, StringComparer.Ordinal);
    var edges = new List<ConsumerEdge>();
    foreach (string pathValue in paths.Distinct(StringComparer.Ordinal)) {
        string path = pathValue.Replace('\\', '/');
        foreach (ReferenceCount reference in scanner.Count(File.ReadAllText(pathValue))) {
            ContractInput contract = contractsByName[reference.Name];
            if (contract.DefinitionPaths.Contains(path, StringComparer.OrdinalIgnoreCase)) continue;
            edges.Add(new ConsumerEdge(
                contract.Name,
                contract.Roles,
                contract.DefinitionPaths,
                Area(path),
                path,
                IsTest(path),
                reference.Count));
        }
    }
    ConsumerEdge[] sortedEdges = edges
        .OrderBy(edge => edge.Contract, StringComparer.CurrentCultureIgnoreCase)
        .ThenBy(edge => edge.IsTest)
        .ThenBy(edge => edge.ConsumerPath, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();
    HashSet<string> consumed = sortedEdges.Select(edge => edge.Contract).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var summary = new BackendSummary(
        contracts.Length,
        contracts.Count(contract => contract.Ambiguous),
        sortedEdges.Length,
        sortedEdges.Count(edge => !edge.IsTest),
        sortedEdges.Count(edge => edge.IsTest),
        consumed.Count,
        contracts.Count(contract => !consumed.Contains(contract.Name)));
    return new BackendIndex(1, summary, contracts, sortedEdges);
}

static string CanonicalJson<T>(T value, JsonSerializerOptions serializerOptions) {
    string compact = JsonSerializer.Serialize(value, serializerOptions);
    var builder = new System.Text.StringBuilder(compact.Length + compact.Length / 4);
    int indent = 0;
    bool inString = false;
    bool escaped = false;
    foreach (char character in compact) {
        if (inString) {
            builder.Append(character);
            if (escaped) escaped = false;
            else if (character == '\\') escaped = true;
            else if (character == '"') inString = false;
            continue;
        }
        if (character == '"') {
            inString = true;
            builder.Append(character);
            continue;
        }
        switch (character) {
            case '{':
            case '[':
                builder.Append(character).Append('\n').Append(' ', ++indent * 2);
                break;
            case '}':
            case ']':
                builder.Append('\n').Append(' ', --indent * 2).Append(character);
                break;
            case ',':
                builder.Append(',').Append('\n').Append(' ', indent * 2);
                break;
            case ':':
                builder.Append(": ");
                break;
            default:
                builder.Append(character);
                break;
        }
    }
    return builder.Append('\n').ToString();
}

static string Area(string path) {
    if (path.StartsWith("MailInbox/", StringComparison.OrdinalIgnoreCase)) return "MailInbox";
    if (path.StartsWith("MailRelay/", StringComparison.OrdinalIgnoreCase)) return "MailRelay";
    if (path.StartsWith("Shared/", StringComparison.OrdinalIgnoreCase)) return "Shared";
    return "FoodDiary";
}

static bool IsTest(string path) =>
    Regex.IsMatch(path, @"(^|/)(tests|[^/]+\.Tests)/|Tests?\.cs$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

sealed class MultiPatternScanner {
    private readonly string[] names;
    private readonly List<Node> nodes = [new Node()];

    public MultiPatternScanner(IEnumerable<string> candidateNames) {
        names = candidateNames
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < names.Length; index++) Add(names[index], index);
        BuildFailureLinks();
    }

    public IReadOnlyList<ReferenceCount> Count(string content) {
        var counts = new int[names.Length];
        int state = 0;
        for (int position = 0; position < content.Length; position++) {
            char value = content[position];
            while (state != 0 && !nodes[state].Transitions.ContainsKey(value)) state = nodes[state].Failure;
            if (nodes[state].Transitions.TryGetValue(value, out int next)) state = next;
            foreach (int nameIndex in nodes[state].Outputs) {
                int start = position - names[nameIndex].Length + 1;
                if ((start == 0 || !IsAsciiIdentifierCharacter(content[start - 1])) &&
                    (position + 1 == content.Length || !IsAsciiIdentifierCharacter(content[position + 1]))) {
                    counts[nameIndex]++;
                }
            }
        }

        var result = new List<ReferenceCount>();
        for (int index = 0; index < names.Length; index++) {
            if (counts[index] > 0) result.Add(new ReferenceCount(names[index], counts[index]));
        }
        return result;
    }

    private void Add(string name, int nameIndex) {
        int state = 0;
        foreach (char value in name) {
            if (!nodes[state].Transitions.TryGetValue(value, out int next)) {
                next = nodes.Count;
                nodes[state].Transitions[value] = next;
                nodes.Add(new Node());
            }
            state = next;
        }
        nodes[state].Outputs.Add(nameIndex);
    }

    private void BuildFailureLinks() {
        var queue = new Queue<int>();
        foreach (int child in nodes[0].Transitions.Values) {
            queue.Enqueue(child);
            nodes[child].Failure = 0;
        }
        while (queue.Count > 0) {
            int state = queue.Dequeue();
            foreach ((char value, int child) in nodes[state].Transitions) {
                queue.Enqueue(child);
                int failure = nodes[state].Failure;
                while (failure != 0 && !nodes[failure].Transitions.ContainsKey(value)) failure = nodes[failure].Failure;
                if (nodes[failure].Transitions.TryGetValue(value, out int target) && target != child) failure = target;
                nodes[child].Failure = failure;
                nodes[child].Outputs.AddRange(nodes[failure].Outputs);
            }
        }
    }

    private static bool IsAsciiIdentifierCharacter(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_';

    private sealed class Node {
        public Dictionary<char, int> Transitions { get; } = [];
        public List<int> Outputs { get; } = [];
        public int Failure { get; set; }
    }
}

sealed record ScannerInput(string[] Paths, string[]? Names = null, ContractInput[]? Contracts = null);
sealed record FileReferenceResult(string Path, IReadOnlyList<ReferenceCount> References);
sealed record ReferenceCount(string Name, int Count);
sealed record ContractInput(string Name, string[] Roles, string[] Kinds, string[] Areas, string[] DefinitionPaths, bool Ambiguous);
sealed record ConsumerEdge(string Contract, string[] Roles, string[] DefinitionPaths, string ConsumerArea, string ConsumerPath, bool IsTest, int ReferenceCount);
sealed record BackendSummary(int Contracts, int AmbiguousContracts, int ConsumerEdges, int ProductionConsumerEdges, int TestConsumerEdges, int ConsumedContracts, int UnconsumedContracts);
sealed record BackendIndex(int SchemaVersion, BackendSummary Summary, ContractInput[] Contracts, ConsumerEdge[] ConsumerEdges);
