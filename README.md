# LearningEdge

LearningEdge is a multi-tenant training platform. An organization owns categories, trainings, files, question banks, and exams. People sign in through Keycloak. Course data lives in SQL Server. Searchable document meaning lives in Qdrant, one collection per organization.

An organization admin can start extraction on a PDF or Word file. That single action crosses the Angular UI, the .NET API, Keycloak, SQL Server, Redis, RabbitMQ, the document parser, Ollama, Qdrant, and SignalR. The question-generator contract exists. Its consumer does not generate questions yet.

The interactive walkthrough of the same flow is in [LearningEdge_Architecture_Lab.html](LearningEdge_Architecture_Lab.html).

## Business rules

| Rule | What it means |
| --- | --- |
| R1 | Vectors for a file go only into that organization's Qdrant collection, `le_docs_{organizationId:N}`. |
| R2 | File extraction starts only for a SysAdmin, or an OrgAdmin of that organization. Instructor is not enough. |
| R3 | If Redis already says the file extraction is `Completed`, a second delivery of the same message is skipped. |
| R4 | Workers write Redis. Only the API speaks SignalR. |
| R5 | A learner stays out of Keycloak until an admin approves them. |

Roles are `Learner`, `Instructor`, `OrgAdmin`, and `SysAdmin`. A realm role is not enough for an extract: the handler also checks that the training belongs to the caller's organization.

## Technical stack

| Layer | Choice |
| --- | --- |
| UI | Angular 22, standalone components, signals, Tailwind, Keycloak JS |
| API | ASP.NET Core on .NET 10, clean architecture |
| Inside the API | MediatR, FluentValidation, AutoMapper, EF Core, JWT bearer, SignalR, MassTransit, Scalar |
| Identity | Keycloak 26, realm `learningedge`, backed by Postgres 16 |
| System of record | SQL Server 2022, database `LearningEdgeDB` |
| Job board | Redis 7.2. Extract status and percent, one hour TTL |
| Broker | RabbitMQ 3, via MassTransit |
| Documents | Document parser reads PDF and DOCX, embeds with Ollama `qwen3-embedding:0.6b`, stores chunks in Qdrant |
| Generation model | `llama3.1:8b` is pulled for later question generation. The generator consumer does not call it yet |
| Observability | Grafana, Prometheus, Loki, and Promtail under `monitoring/` |

The API route shape is `/api/v1/{controller}`. Controllers are singular and lowercase, for example `/api/v1/user` and `/api/v1/organization`.

## Repository layout

```text
LearningEdge/                 .NET solution
  src/LearningEdge.Api/       Controllers, JWT, SignalR hub, composition root
  src/LearningEdge.Application/  Commands, queries, handlers, validators, ports
  src/LearningEdge.Domain/    Entities and domain rules
  src/LearningEdge.Infrastructure/  EF Core, Redis, Keycloak admin client, consumers
  src/LearningEdge.Infrastructure.Migrations/
  src/LearningEdge.Messaging.Contracts/  The only shared message surface
  tests/LearningEdge.Tests/
  aspires/                    .NET Aspire app host and service defaults
LearningEdgeUI/               Angular app (package name tms-ui)
LearningEdgeMS/               Document parser worker
LearningEdgeQGS/              Question generator worker (stub, not in Compose)
keycloak/themes/learningedge/ Sign-in theme
docker/                       SQL init scripts and RabbitMQ config
monitoring/                   Grafana, Prometheus, Loki, Promtail, cAdvisor, SQL exporter
```

Dependency direction: Domain has no infrastructure. Application defines the cache, file, and Keycloak ports. Infrastructure and the workers implement them. Workers do not reference the API project. They share SQL Server, Redis, RabbitMQ, and the uploads volume.

## Core components

**LearningEdge.Api** is one process: controllers, MediatR handlers, EF Core, the MassTransit host for completion messages, and the SignalR hub at `/hubs/file-extract`. A background publisher reads Redis every 500 ms and pushes only when the watched file's status or percent changed.

**Application and Domain** hold the catalog and the rules. The entities are Organization, User, Category, Training, TrainingFile, QuestionBank, Question (MCQ, multi-select, true/false), PromptRequest, Exam, and ExamQuestion.

**Infrastructure** is EF Core against SQL Server, the Redis cache, the Keycloak admin client, and the extract-completed consumer.

**Messaging.Contracts** defines `IExtractRequestSubmitted`, `IExtractRequestCompleted`, `IQuestionGenerationSubmitted`, and `IQuestionGenerationCompleted`.

**DocumentParserService** consumes the extract request, reads the file from the shared uploads volume, embeds with Ollama, upserts Qdrant, and publishes completion.

**QuestionGeneratorService** is registered as a consumer. The body waits one second and returns. Compose does not start this container. The API can save a prompt row and publish `IQuestionGenerationSubmitted`. The completion consumer on the API is not wired.

**Angular UI** talks only to the API. It sends the Keycloak access token as `Authorization: Bearer` and the active organization as `X-Organization-Id`. In Docker, nginx on port 4300 serves the bundle. The browser still calls the API at `https://localhost:5001` and Keycloak at `https://localhost:8443`.

## Process flows

### File extraction

1. The signed-in UI calls the API over HTTPS. JwtBearer checks issuer, audience (`le-web-api`), and signature against the `learningedge` realm. The API does not issue tokens.
2. `UpdateTrainingInitiateFileExtractCommand` loads the training. The caller must be a SysAdmin, or an OrgAdmin of that organization.
3. The handler publishes `IExtractRequestSubmitted` to `le-file-extract-requested`, then writes Redis `Queued` and progress `0` for one hour. The file bytes are already on the uploads volume. The HTTP call returns before parsing starts.
4. The UI calls `Watch(fileId)`. The hub joins group `file-extract:{fileId}` and sends the current Redis snapshot to that caller.
5. The parser reads Redis. `Completed` means stop (R3). Otherwise it sets `Processing`, reads the file under the uploads root, and marks extraction started on the `TrainingFile` row.
6. `.pdf` and `.docx` are supported. A missing file or empty text becomes a failed completion. Qdrant is not touched. There is no OCR.
7. Each sentence is embedded with `qwen3-embedding:0.6b`. A cosine similarity drop below `0.72` closes the chunk. Each chunk gets its own vector. Redis progress is the percent of chunks finished. This is the slow step.
8. The collection name is `le_docs_` plus the organization id with dashes removed. Each point stores the chunk text, chunk type, a timestamp, and `file_id`.
9. The parser sets Redis to `Completed` and publishes `IExtractRequestCompleted`. The API consumer records success or failure on the `TrainingFile`. The parser does not update the UI.
10. The publisher pushes to the SignalR group only when status and percent differ from the last push.

SQL is the system of record. Redis is the board a browser reads. They can disagree for a short time. A message can arrive twice, so consumers must notice `Completed` and stop.

### Identity

Keycloak keeps credentials in its own Postgres database. LearningEdge keeps the profile, role, and organization in SQL Server. The link is `User.KeycloakUserId`.

Self-registration writes a SQL user and holds the password with a reversible protector. It does not call Keycloak, so the person cannot sign in. An OrgAdmin of that organization, or a SysAdmin, approves. The API's confidential client creates the Keycloak user, sets the password, assigns the learner realm role, stores the Keycloak id, and clears the protected password. If Keycloak rejects the call, the handler does not leave an active LearningEdge user that can never sign in.

A system admin creates an organization admin in both stores in one command. SysAdmin accounts themselves are created in Keycloak. The app has no "create system admin" command.

On each request, roles inside the token's `realm_access` JSON are copied onto role claims. `SysAdmin` also matches the aliases defined in `KeycloakRoles`.

### Question prompts

The prompt row is saved in SQL before publish. Publish uses an 8 second timeout. If RabbitMQ does not accept it, the row stays and the API returns a publish failure. There is no outbox, so retrying the HTTP call can create a second prompt row. Nothing in the generator consumer searches Qdrant or writes questions yet.

## Run with Docker

Copy `.env.example` to `.env` and set the SQL and certificate passwords. The Compose file also expects Keycloak, RabbitMQ, and AutoMapper license values in that file. Certificates used by the API and Keycloak live in `certs/`.

From the repository root, on the external network `le-shared-net`:

```powershell
docker network create le-shared-net
docker compose up -d --build
```

`docker compose up -d` reuses images that already exist. Pass `--build` after UI or API source changes, or the container keeps serving the previous bundle.

`redis-commander` is on the `manual` profile:

```powershell
docker compose --profile manual up -d redis-commander
```

Monitoring is a separate Compose file. Its network must be the same external `le-shared-net`, or the SQL exporter cannot resolve `sqlserver`.

```powershell
docker compose -f monitoring/docker-compose.yml up -d
```

### Ports

Containers on the Compose network use service DNS names and container ports. These host ports are for tools on Windows.

| Service | Host port |
| --- | --- |
| Angular (`ng serve`) | 4200 |
| Angular through nginx | 4300 |
| API | 5000 HTTP, 5001 HTTPS |
| Keycloak | 8443 |
| Keycloak Postgres | 5432 |
| SQL Server | 11433 → container 1433 |
| Redis | 6380 → container 6379 |
| Redis Commander | 8081 |
| RabbitMQ | 5672, management UI 15672 |
| Qdrant | 6333 REST, 6334 gRPC |
| Ollama | 11434 |
| Grafana | 3000 |
| Prometheus | 9090 |
| Loki | 3100 |

Inside Docker, the API loads Keycloak signing keys from `https://keycloak:8443` and still accepts tokens whose issuer is `https://localhost:8443/realms/learningedge`. The UI requests the `le-api-audience` scope so the access token audience is `le-web-api`. After an image rebuild, sign out and sign in again so Keycloak issues a token with that audience.

## Local UI without Docker

```powershell
cd LearningEdgeUI
npm run setup:env
npm start
```

`npm run setup:env` copies `environment.development.example.ts` to the gitignored `environment.development.ts`. The dev server is `http://localhost:4200`.
