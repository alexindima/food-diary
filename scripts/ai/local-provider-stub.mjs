import { createServer } from "node:http";

const port = Number(process.argv[2]);
const task = process.argv[3];
if (
    !Number.isInteger(port) ||
    port < 1024 ||
    port > 65535 ||
    !/^[a-f0-9-]{36}$/u.test(task ?? "")
)
    throw new Error("Use an owned task provider port and task identity");

createServer((request, response) => {
    response.setHeader("content-type", "application/json");
    if (request.url === "/health") {
        response.end(
            JSON.stringify({ task, status: "ready", providers: "disabled" }),
        );
        return;
    }
    // Consume no content and retain no mail, identities, provider payloads or headers.
    response.writeHead(501);
    response.end(JSON.stringify({ code: "Task.ProviderDisabled" }));
}).listen(port, "127.0.0.1");
