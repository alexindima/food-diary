using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using LlmWiki.SqliteReader;
using Microsoft.Data.Sqlite;
using SQLitePCL;

Batteries_V2.Init();
string root = Path.Combine(AppContext.BaseDirectory, "owned-fixture");
Directory.CreateDirectory(Path.Combine(root, ".llm-wiki/generated"));
Directory.CreateDirectory(Path.Combine(root, ".artifacts/llm-wiki/code-graph"));
foreach (string file in new[] { "runtime-topology.json", "domain-data-index.json" }) {
    File.WriteAllText(Path.Combine(root, ".llm-wiki/generated", file), "{}");
}
string expectedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("{}")));
string database = Path.Combine(root, ".artifacts/llm-wiki/code-graph/code-graph.sqlite");
string connectionString = new SqliteConnectionStringBuilder { DataSource = database, Pooling = false, DefaultTimeout = 2 }.ToString();
using (var setup = new SqliteConnection(connectionString)) {
    setup.Open();
    using var command = setup.CreateCommand();
    command.CommandText = """
        PRAGMA journal_mode=WAL;
        DROP TABLE IF EXISTS metadata;
        DROP TABLE IF EXISTS query_documents;
        CREATE TABLE metadata(key TEXT PRIMARY KEY,value TEXT);
        CREATE TABLE query_documents(category TEXT,record_kind TEXT,source_ordinal INTEGER,payload_json TEXT);
        INSERT INTO metadata VALUES('query_source:runtime',$hash),('query_source:domain',$hash);
        INSERT INTO query_documents VALUES('runtime','composeService',0,'{"generation":"A"}'),
          ('domain','domainType',0,'{"generation":"A"}'),
          ('domain','invariant',1,'{"generation":"A"}'),
          ('domain','persistenceMapping',2,'{"generation":"A"}');
        WITH RECURSIVE n(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM n WHERE x<10000)
          INSERT INTO query_documents SELECT 'unrelated','padding',x,'{}' FROM n;
        """;
    command.Parameters.AddWithValue("$hash", expectedHash);
    command.ExecuteNonQuery();
}
using var stop = new CancellationTokenSource();
Exception? writerFailure = null;
var writer = new Thread(() => {
    try {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        while (!stop.IsCancellationRequested) {
            foreach (string generation in new[] { "B", "A" }) {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    BEGIN IMMEDIATE;
                    UPDATE metadata SET value=$hash;
                    UPDATE query_documents SET payload_json=$payload WHERE category IN ('domain','runtime');
                    COMMIT;
                    """;
                command.Parameters.AddWithValue("$hash", generation == "A" ? expectedHash : new string('b', 64));
                command.Parameters.AddWithValue("$payload", "{\"generation\":\"" + generation + "\"}");
                command.ExecuteNonQuery();
                Thread.Sleep(1);
            }
        }
    } catch (Exception error) { writerFailure = error; }
}) { IsBackground = true };
writer.Start();
var observations = new List<object>();
int mixedResponses = 0;
try {
    foreach (string route in new[] { "runtime", "domain" }) {
        int valid = 0, rejected = 0, mixed = 0;
        for (int index = 0; index < 1000; index++) {
            string json;
            try {
                json = route == "runtime" ? CompiledIndexReader.QueryRuntime(root, "", 30, true)
                    : DomainDataReader.Query(root, "all", "", 30, true);
            } catch (InvalidOperationException error) when (error.Message.Contains("stale", StringComparison.Ordinal)) {
                rejected++;
                continue;
            }
            valid++;
            using var document = JsonDocument.Parse(json);
            bool wrong = false;
            foreach (JsonProperty property in document.RootElement.EnumerateObject()) {
                if (property.Name.StartsWith('_')) { continue; }
                foreach (JsonElement item in property.Value.EnumerateArray()) {
                    if (item.GetProperty("generation").GetString() != "A") { wrong = true; }
                }
            }
            if (wrong) { mixed++; }
        }
        mixedResponses += mixed;
        observations.Add(new { route, validResponses = valid, staleRejections = rejected, responsesFromUnverifiedGeneration = mixed });
    }
} finally {
    stop.Cancel();
    if (!writer.Join(5000)) { throw new InvalidOperationException("Owned writer did not stop."); }
}
if (writerFailure is not null) { throw writerFailure; }
Console.WriteLine(JsonSerializer.Serialize(observations));

if (mixedResponses != 0) { throw new InvalidOperationException("Reader returned payloads from an unverified projection generation."); }
Console.WriteLine("Projection WAL snapshot stress passed.");
