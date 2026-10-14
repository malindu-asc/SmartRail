---
name: smartrail-modular-monolith
description: Use when adding or changing SmartRail backend code - creating a module, a command or query, an entity and its EF Core mapping, a migration, an endpoint, a domain or integration event, or when asked about module boundaries, layer rules or how modules talk to each other. .NET modular monolith with Clean Architecture per module, CQRS via MediatR, PostgreSQL schema per module, and in-process events through a transactional outbox.
---

# SmartRail backend - modular monolith

> **Examples are for reference only.** Every code block in this file shows a *pattern*. Entity names, fields, routes and values are illustrative. Take the real names, fields and rules from `docs/00-requirements` (requirements, use cases, state models) and adapt the pattern to them. Never paste an example in as the implementation.

Source: adapted from the `ashankasr/modular-monolith-eventdriven` blueprint skills (architecture, cqrs-patterns, domain-ef, create-module, implement-feature, integration-events-consumers, presentation-endpoints).

| Kept from the blueprint | Dropped | Changed for SmartRail |
|---|---|---|
| 5 projects per module, layer rules | RabbitMQ, MassTransit, `IConsumer<T>` | `IntegrationEvents` -> `Contracts`, `Presentation` -> `Api` |
| CQRS with MediatR, `Result`/`Error` | Sagas, orchestration, compensation consumers | Events are **in-process only** |
| Module-specific unit of work | Choreography over a broker | SQL Server -> **PostgreSQL** (Npgsql) |
| Schema per module, design-time factory | Quartz | `BackgroundService` polls the outbox |
| Transactional outbox + domain events | `ConfigureConsumers` | "DDD" framing -> plain Clean Architecture |
| FluentValidation, Mapster, Scalar | | Fixes listed in "Fixes over the blueprint" |

---

## 1. Solution layout

```
src/
  Host/
    SmartRail.AppHost               Aspire: Postgres, Api, Node catering, frontends
    SmartRail.ServiceDefaults       OpenTelemetry, health checks, resilience
    SmartRail.Api                   composition root: modules, behaviours, outbox worker, auth, SignalR
  BuildingBlocks/
    SmartRail.SharedKernel          Entity, AuditableEntity, IDomainEvent, DomainEvent, Result, Error, IRepository, IUnitOfWork
    SmartRail.Application.Core      ICommand, IQuery, handlers, IDomainEventHandler, IIntegrationEvent(Handler), IEventBus, behaviours
    SmartRail.Infrastructure.Core   BaseDbContext, BaseRepository, outbox (message, interceptor, processor, worker), InProcessEventBus
  Modules/<Name>/
    SmartRail.Modules.<Name>.Domain
    SmartRail.Modules.<Name>.Application
    SmartRail.Modules.<Name>.Infrastructure
    SmartRail.Modules.<Name>.Api
    SmartRail.Modules.<Name>.Contracts
services/catering                   Node.js, external, own database
tools/Simulator                     posts through the real ingestion endpoint
tests/
  SmartRail.ArchitectureTests
  SmartRail.Modules.<Name>.Tests
```

Modules: **Identity** (users, roles, permissions, audit), **Network** (stations, routes, timetable versions, driver and unit assignments), **Journeys** (live state, confidence, ingestion, departure board), **Operations** (cancel, terminate, platform change, disruptions, driver messages, incidents), **Notifications** (realtime push, follows), **Ticketing** (stretch only).

---

## 2. Dependency rules

```
Api             -> Application -> Domain -> SharedKernel
Infrastructure  -> Application, Domain, Infrastructure.Core
Application     -> Domain, Application.Core, other modules' Contracts
Contracts       -> SharedKernel only (records and interfaces, no logic)
Host (Api)      -> every module's Infrastructure (Add<Module>Module) and Api (Map<Module>Endpoints)
```

| Reference | Allowed |
|---|---|
| Module A -> Module B `Contracts` | Yes |
| Module A -> Module B `Domain`, `Application`, `Infrastructure`, `Api` | **Never** |
| `Api` -> own `Domain` or `Infrastructure` | Never; use Application DTOs |
| `Domain` -> EF Core, MediatR, ASP.NET, `Application.Core` | Never |
| Repository querying another module's DbContext or schema | Never |

`SmartRail.ArchitectureTests` (NetArchTest or ArchUnitNET) enforces every row above and fails the build.

---

## 3. Building blocks

### SharedKernel

```
Entity<TKey>                id, domain events, equality by id
  AuditableEntity<TKey>     + CreatedAtUtc, UpdatedAtUtc (set by BaseDbContext)
    AuditableEntity         shorthand for AuditableEntity<Guid>
IDomainEvent / DomainEvent  EventId, OccurredOnUtc
Result / Result<T>          success or failure with Error; implicit T -> Result<T>
Error(Code, Description, Type)   Type: Failure, Validation, NotFound, Conflict, Forbidden
IRepository<T, TKey>        GetByIdAsync, Add, Remove
IUnitOfWork                 SaveChangesAsync
```

```csharp
// Reference only
public enum ErrorType { Failure, Validation, NotFound, Conflict, Forbidden }

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static Error NotFound(string code, string description)   => new(code, description, ErrorType.NotFound);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error Conflict(string code, string description)   => new(code, description, ErrorType.Conflict);
    public static Error Forbidden(string code, string description)  => new(code, description, ErrorType.Forbidden);
    public static Error Failure(string code, string description)    => new(code, description, ErrorType.Failure);
}
```

### Application.Core

```
ICommand / ICommand<T>              : IRequest<Result> / IRequest<Result<T>>
IQuery<T>                           : IRequest<Result<T>>
ICommandHandler<..>, IQueryHandler<..>
DomainEventNotification<T>          MediatR INotification wrapper (keeps MediatR out of Domain)
IDomainEventHandler<T>              : INotificationHandler<DomainEventNotification<T>>
IIntegrationEvent                   EventId, OccurredOnUtc
IIntegrationEventHandler<T>         Task Handle(T, CancellationToken)
IEventBus                           Task PublishAsync<T>(T, CancellationToken) where T : IIntegrationEvent
Behaviors/LoggingBehavior, ValidationBehavior
```

### Infrastructure.Core

```
BaseDbContext : DbContext, IUnitOfWork   audit timestamps, OutboxMessages DbSet
BaseRepository<T, TKey, TContext>
Outbox/OutboxMessage                     Id, Type, Content, OccurredOnUtc, ProcessedOnUtc, Attempts, Error
Outbox/OutboxMessagesInterceptor         domain events -> outbox rows in the same transaction
Outbox/OutboxMessageProcessor<TContext>  one per module
Outbox/OutboxWorker : BackgroundService  polls every processor
EventBus/InProcessEventBus : IEventBus   resolves IIntegrationEventHandler<T> and awaits each
```

---

## 4. Domain layer

Plain C# entities with behaviour. No framework references. Use aggregates, child entities and value objects only where they help; this is not a DDD project.

| Need | Use |
|---|---|
| Has identity and lifecycle | `sealed class : AuditableEntity` |
| Owned by another entity, no own lifecycle | `sealed class : Entity<Guid>` |
| Data grouping with no identity | `sealed record` (map with `OwnsOne`/`OwnsMany`) |
| Fixed set of states | `enum` with explicit values, stored as string |
| Something happened that something else reacts to | domain event |

Rules:
- `sealed class`, `private` parameterless constructor for EF, `private set` on every property.
- `static Create(...)` validates and returns `Result<T>`; it never throws for business failures.
- Behaviour methods are named in business language (`RecordDeparture`, not `SetStatus`) and return `Result` when they can fail. Do not throw for expected failures.
- Time is passed in (from `TimeProvider`/`IClock`), never read with `DateTime.UtcNow` inside the entity.
- Errors live in `Domain/Errors/<Module>Errors.cs`; codes are `<Module>.<PascalCaseReason>`.

```csharp
// Reference only - illustrative fields
public sealed class Journey : AuditableEntity
{
    private Journey() { }

    public string ServiceCode { get; private set; } = string.Empty;
    public JourneyStatus Status { get; private set; }
    public DateTimeOffset? LastEventAtUtc { get; private set; }

    public Result RecordDeparture(Guid stopId, DateTimeOffset at, EventSource source)
    {
        if (Status is JourneyStatus.Cancelled or JourneyStatus.Completed)
            return Result.Failure(JourneysErrors.NotRunning(Id));

        // apply source precedence, ignore duplicates, update state ...
        LastEventAtUtc = at;
        RaiseDomainEvent(new JourneyStateChangedDomainEvent(Id, stopId, at));
        return Result.Success();
    }
}

public static class JourneysErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Journeys.NotFound", $"Journey '{id}' was not found.");
    public static Error NotRunning(Guid id) =>
        Error.Conflict("Journeys.NotRunning", $"Journey '{id}' is not running.");
}

public sealed record JourneyStateChangedDomainEvent(Guid JourneyId, Guid StopId, DateTimeOffset At) : DomainEvent;
```

Repository interface sits in Domain and extends `IRepository<T, TKey>` from SharedKernel.

---

## 5. Application layer (CQRS)

| Rule | Detail |
|---|---|
| Command | `sealed record ...Command(...) : ICommand` or `ICommand<T>`; data only |
| Query | `sealed record ...Query(...) : IQuery<T>`; response DTOs in the same file |
| Handler | `sealed class`, primary constructor, `ICommandHandler`/`IQueryHandler` (not `IRequestHandler`) |
| Success | `return value;` (implicit) or `Result.Success()` |
| Failure | `return Result.Failure<T>(Errors.X)` |
| Unit of work | Commands inject **own** `I<Module>UnitOfWork`; queries never do |
| Reads | Queries may use `AsNoTracking()` projections; never return entities |
| Validation | One `AbstractValidator<TCommand>` per command with input; auto-discovered |
| Mapping | Manual for flat DTOs (1-4 fields); Mapster for nested/enum/collections |
| Side effects | Never push SignalR, call catering, or touch another module from a command handler; raise a domain event |

Folder convention (one folder per use case):

```
Application/
  Abstractions/IJourneysUnitOfWork.cs
  Journeys/
    RecordDeparture/
      RecordDepartureCommand.cs
      RecordDepartureCommandHandler.cs
      RecordDepartureCommandValidator.cs
    GetDepartureBoard/
      GetDepartureBoardQuery.cs          query + response records
      GetDepartureBoardQueryHandler.cs
    JourneyStateChangedDomainEventHandler.cs
  IntegrationEventHandlers/              handlers for other modules' events
  AssemblyReference.cs
```

```csharp
// Reference only
public sealed record RecordDepartureCommand(Guid JourneyId, Guid StopId, DateTimeOffset At, EventSource Source) : ICommand;

public sealed class RecordDepartureCommandHandler(
    IJourneyRepository journeys,
    IJourneysUnitOfWork unitOfWork) : ICommandHandler<RecordDepartureCommand>
{
    public async Task<Result> Handle(RecordDepartureCommand command, CancellationToken ct)
    {
        var journey = await journeys.GetByIdAsync(command.JourneyId, ct);
        if (journey is null)
            return Result.Failure(JourneysErrors.NotFound(command.JourneyId));

        var result = journey.RecordDeparture(command.StopId, command.At, command.Source);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(ct);   // outbox rows saved in the same transaction
        return Result.Success();
    }
}

public sealed class RecordDepartureCommandValidator : AbstractValidator<RecordDepartureCommand>
{
    public RecordDepartureCommandValidator()
    {
        RuleFor(x => x.JourneyId).NotEmpty();
        RuleFor(x => x.StopId).NotEmpty();
    }
}
```

```csharp
// Reference only
public sealed record GetDepartureBoardQuery(Guid StationId) : IQuery<DepartureBoardResponse>;
public sealed record DepartureBoardResponse(Guid StationId, DateTimeOffset AsOfUtc, IReadOnlyList<DepartureRow> Rows);
public sealed record DepartureRow(Guid JourneyId, string Destination, DateTimeOffset Scheduled,
    DateTimeOffset? Expected, string? Platform, string Status, string Confidence);
```

Pipeline behaviours, in order: `LoggingBehavior` -> `ValidationBehavior` -> handler. They are registered **once** in the host, not per module (see section 9).

---

## 6. Persistence (EF Core + PostgreSQL)

### DbContext

```csharp
// Reference only
public sealed class JourneysDbContext(DbContextOptions<JourneysDbContext> options)
    : BaseDbContext(options), IJourneysUnitOfWork
{
    public DbSet<Journey> Journeys => Set<Journey>();          // DbSets for roots only

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);                    // maps OutboxMessages - always first
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JourneysDbContext).Assembly);
    }

    public const string Schema = "journeys";
}
```

### Entity configuration

One `sealed class <Entity>Configuration : IEntityTypeConfiguration<T>` per entity in `Infrastructure/Persistence/Configurations/`. No data annotations on entities.

```csharp
// Reference only
public sealed class JourneyConfiguration : IEntityTypeConfiguration<Journey>
{
    public void Configure(EntityTypeBuilder<Journey> builder)
    {
        builder.ToTable("journeys");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.ServiceCode).IsRequired().HasMaxLength(50);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(j => j.ServiceCode);
    }
}
```

| Concern | Convention |
|---|---|
| Name / title | `HasMaxLength(200)` |
| Code / short reference | `HasMaxLength(50)` |
| Email | `HasMaxLength(200)` |
| Reason / message | `HasMaxLength(500)` |
| Long text | `HasMaxLength(1000)` |
| Enum | `.HasConversion<string>()`; explicit integer values in the enum |
| Decimal | `HasPrecision(18, 2)` |
| Time | `DateTimeOffset` in UTC (`timestamptz`) |
| Child collection | `HasMany(...).WithOne().HasForeignKey(...)`; no back-navigation needed |
| Value object | `OwnsOne` (columns on owner) / `OwnsMany` (own table, shadow FK) |
| Concurrency (controller edits) | `xmin` row version via `UseXminAsConcurrencyToken()` or an explicit version column |

### Design-time factory and migrations

Each module has `<Module>DbContextFactory : IDesignTimeDbContextFactory<T>` so `dotnet ef` works without the host. The migrations history table **must** live in the module's schema, both in the factory and in the runtime registration.

```csharp
// Reference only
public sealed class JourneysDbContextFactory : IDesignTimeDbContextFactory<JourneysDbContext>
{
    public JourneysDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("SMARTRAIL_DB")
                 ?? "Host=localhost;Database=smartrail;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<JourneysDbContext>()
            .UseNpgsql(cs, b => b.MigrationsHistoryTable("__EFMigrationsHistory", JourneysDbContext.Schema))
            .Options;
        return new JourneysDbContext(options);
    }
}
```

```bash
dotnet ef migrations add <Name> --project src/Modules/<Module>/SmartRail.Modules.<Module>.Infrastructure --context <Module>DbContext --output-dir Persistence/Migrations
dotnet ef database update --project src/Modules/<Module>/SmartRail.Modules.<Module>.Infrastructure --context <Module>DbContext
dotnet ef migrations script --project src/Modules/<Module>/SmartRail.Modules.<Module>.Infrastructure --context <Module>DbContext
dotnet ef migrations remove --project src/Modules/<Module>/SmartRail.Modules.<Module>.Infrastructure --context <Module>DbContext
```

- Review every generated migration for unintended drops.
- `Database.MigrateAsync()` on startup in **Development only**.
- Repositories never call `SaveChanges`; that is the unit of work's job.

---

## 7. Events (in-process only)

### Two kinds

| | Domain event | Integration event |
|---|---|---|
| Defined in | own `Domain/Events` | own `Contracts` |
| Implements | `DomainEvent` | `IIntegrationEvent` |
| Handled by | `IDomainEventHandler<T>` in own Application | `IIntegrationEventHandler<T>` in **another** module's Application |
| Purpose | react inside the module; translate to integration event | let other modules react |

### Flow

```
entity.RaiseDomainEvent(...)                       in memory
unitOfWork.SaveChangesAsync()
  OutboxMessagesInterceptor                        writes OutboxMessage rows, same transaction
OutboxWorker (BackgroundService, every 1-2 s)
  OutboxMessageProcessor<TContext>                 unprocessed rows, oldest first, batch of 20
    publish DomainEventNotification<T>             MediatR, in-process
      IDomainEventHandler<T>
        eventBus.PublishAsync(IntegrationEvent)    InProcessEventBus
          IIntegrationEventHandler<T> in other modules   (Notifications pushes via SignalR)
  success -> ProcessedOnUtc = now
  failure -> Attempts++, Error = ex; ProcessedOnUtc stays null -> retried next poll
             Attempts >= max -> mark dead-lettered, log error, surface in device/data health
```

```csharp
// Reference only - domain event handler translates to an integration event
public sealed class JourneyStateChangedDomainEventHandler(IEventBus eventBus)
    : IDomainEventHandler<JourneyStateChangedDomainEvent>
{
    public Task Handle(DomainEventNotification<JourneyStateChangedDomainEvent> n, CancellationToken ct) =>
        eventBus.PublishAsync(new JourneyStateChangedIntegrationEvent(
            n.DomainEvent.JourneyId, n.DomainEvent.At), ct);
}

// Contracts project of Journeys
public sealed record JourneyStateChangedIntegrationEvent(Guid JourneyId, DateTimeOffset At) : IntegrationEvent;

// Notifications.Application/IntegrationEventHandlers
public sealed class PushJourneyStateHandler(IRealtimeNotifier notifier)
    : IIntegrationEventHandler<JourneyStateChangedIntegrationEvent>
{
    public Task Handle(JourneyStateChangedIntegrationEvent e, CancellationToken ct) =>
        notifier.JourneyChangedAsync(e.JourneyId, ct);   // SignalR implementation lives in Infrastructure
}
```

### Rules

1. **Never** publish from a command handler. Entity raises -> outbox -> handler publishes.
2. The outbox catch block **never** sets `ProcessedOnUtc`. Set `Error` and `Attempts` only.
3. Handlers must be **idempotent**: a retry re-runs every handler for that message. Guard with "already applied?" checks.
4. Domain-event handlers do not inject the unit of work for the same change; the change is already committed. They may load data to build the integration event.
5. An integration-event handler that changes state uses **its own** module's unit of work in its own scope.
6. Integration events are `sealed record`s with primitives and nested records only, plus `OccurredOnUtc`. No entities.
7. Keep the event list short and named in the past tense: `JourneyStateChanged`, `JourneyCancelled`, `PlatformChanged`, `DisruptionRaised`, `DriverMessageSent`, `AssignmentChanged`, `DeviceWentOffline`.
8. No sagas. A multi-step flow is commands plus events; undo is a plain command (for example, reinstate after cancel).
9. Running more than one API instance needs `FOR UPDATE SKIP LOCKED` in the processor query. With a single instance this is not needed.

### Decision guide

| Situation | Use |
|---|---|
| Same module reacts | `IDomainEventHandler<T>` |
| Another module reacts | domain event -> `IIntegrationEventHandler<T>` in that module |
| Screens must update live | integration event -> Notifications -> SignalR |
| External system (catering) must know | domain-event handler calls the catering adapter (timeout + circuit breaker) |
| A module needs data from another module now | query interface in the owner's `Contracts`, implemented in the owner's Infrastructure |

---

## 8. Api layer (Minimal API endpoints)

One `<Module>Endpoints.cs` per module, with request records at the bottom of the file and a `Tags.cs`.

```csharp
// Reference only
public static class JourneysEndpoints
{
    public static IEndpointRouteBuilder MapJourneysEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/journeys").WithTags(Tags.Journeys);

        group.MapGet("/boards/{stationId:guid}", async (Guid stationId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDepartureBoardQuery(stationId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
        })
        .AllowAnonymous()
        .WithSummary("Departure board for a station");

        group.MapPost("/{journeyId:guid}/departures", async (Guid journeyId, RecordDepartureRequest request,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RecordDepartureCommand(journeyId, request.StopId, request.At, EventSource.Driver), ct);
            return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
        })
        .RequireAuthorization(Permissions.RecordStopEvents)
        .WithSummary("Record a departure");

        return app;
    }
}

public sealed record RecordDepartureRequest(Guid StopId, DateTimeOffset At);
```

`ApiResults.Problem(result)` (shared helper in the host or Application.Core) maps `Error.Type` to `ProblemDetails`:

| Outcome | HTTP |
|---|---|
| Query success | 200 with DTO |
| Command created a resource | 201 with `Location` |
| Command success, no body | 204 |
| `Validation` / FluentValidation failure | 400 (`ValidationProblemDetails`) |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` (including concurrent edit) | 409 |
| `Failure` / unhandled exception | 500, no stack trace in the body |

Rules:
- Endpoints bind, send, map. No business rules.
- Request records are `sealed record` with primitives only, mapped to the command inside the lambda.
- Always pass the `CancellationToken`.
- Authorise by **permission policy**, not role names.
- Add `.WithSummary(...)` for Scalar/OpenAPI.

---

## 9. Module registration and host wiring

```csharp
// Reference only - Infrastructure/Extensions/JourneysModule.cs
public static class JourneysModule
{
    public static IServiceCollection AddJourneysModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<JourneysDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("smartraildb"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", JourneysDbContext.Schema))
            .AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>()));

        services.AddScoped<IJourneysUnitOfWork>(sp => sp.GetRequiredService<JourneysDbContext>());
        services.AddScoped<IJourneyRepository, JourneyRepository>();
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<JourneysDbContext>>();
        return services;
    }

    public static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyReference).Assembly;
}
```

```csharp
// Reference only - SmartRail.Api/Program.cs
builder.AddServiceDefaults();

builder.Services.AddJourneysModule(builder.Configuration);
// ... other modules

builder.Services.AddApplicationCore(            // MediatR + validators + behaviours ONCE, all modules
    JourneysModule.ApplicationAssembly /*, other modules */);
builder.Services.AddInfrastructureCore();       // interceptor, InProcessEventBus, OutboxWorker, TimeProvider

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapEndpoints();                             // calls every Map<Module>Endpoints()
```

Aspire AppHost (reference only): add Postgres with a `smartraildb` database, add the Api project with `.WithReference(db).WaitFor(db)`, add the catering Node app with the Aspire Node.js hosting integration, and add the frontends.

---

## 10. Adding a module

1. Create the 5 projects under `src/Modules/<Name>/` and add them to the solution.
2. Add `AssemblyReference.cs` to Application, Infrastructure and Api.
3. Domain: first entity, `<Module>Errors`, repository interface.
4. Application: `I<Module>UnitOfWork`, first command and query.
5. Infrastructure: `<Module>DbContext` (schema = lowercase module name), configuration, repository, design-time factory, `Add<Module>Module`.
6. Api: `Map<Module>Endpoints`, `Tags.cs`.
7. Contracts: only what other modules really need.
8. Host: reference Infrastructure and Api; call `Add<Module>Module`; add the assembly to `AddApplicationCore`; add `Map<Module>Endpoints` to `MapEndpoints()`.
9. Create the `Initial` migration, then `dotnet build` and run the architecture tests.

---

## 11. Implementing a feature

### Phase 1 - brainstorm (write it down and confirm before coding)

- **Operation:** who does what; write or read; which module owns the entity.
- **Domain impact:** state changed; rules and failure cases with their error codes; new entity needed?
- **Event story:** does another module or a screen need to know? Is the external catering service affected?
- **HTTP contract:** verb, route, request, response, error codes.
- **Validation:** required, length, range, business constraints.
- **SmartRail checks:** audit record? permission? freshness/confidence affected?

### Phase 2 - decide which pieces are needed

| Piece | When |
|---|---|
| Entity / configuration / migration | new entity or property |
| Repository method | new query on the aggregate |
| Command + handler + validator | state change |
| Query + handler + DTO | read |
| Domain event | something else must react |
| Domain-event handler | translate to integration event or call an adapter |
| Integration event in Contracts | another module must react |
| Integration-event handler in the other module | that module's reaction |
| Endpoint | callable over HTTP |

### Phase 3 - build order

Domain -> Application -> Infrastructure (configuration, repository, migration) -> Contracts -> event handlers -> Api -> tests. Domain is pure, so it goes first; the endpoint maps to a command that must already exist, so it goes last.

---

## 12. SmartRail rules that shape the code

1. **Honest information:** every journey state carries a confidence (Live, Stale, Unknown) and `AsOfUtc`. Stale after 60 s without data, Unknown after 300 s, device offline after 90 s. Never present stale data as live.
2. **Scheduled time never changes** when a delay is recorded. Expected time and delay are separate fields.
3. **Source precedence and idempotency:** simulator and driver events are ordered by source rank and timestamp; duplicates and out-of-order events are ignored safely. The driver's offline queue replays through the same command.
4. **Every operator change is audited** (who, what, why, when) in the same transaction as the change.
5. **Separation of duties:** the system administrator manages users and roles and cannot operate trains; the controller operates trains and cannot manage users.
6. **Information only:** nothing is safety-critical; no interlocking-style controls.
7. **Concurrent edits:** controller actions use optimistic concurrency; a conflict returns 409 with what changed.
8. **Catering** is an external Node.js service behind an adapter with a timeout and circuit breaker; when it is down the feature degrades and says so.

---

## 13. Fixes over the blueprint

| Blueprint behaviour | SmartRail rule |
|---|---|
| `AddApplication` called per module re-adds the behaviours each time, so validation runs once per module | Register MediatR behaviours **once** in the host |
| `Error` has no type, so every failure maps to 400 | `Error.Type` drives the HTTP status |
| `ValidationBehavior` throws `ValidationException` with no handler shown | Add an `IExceptionHandler` that returns 400 `ValidationProblemDetails` |
| Outbox retries a failing message forever | `Attempts` counter, max attempts, dead-letter flag |
| Poll interval 30 s (Quartz) | 1-2 s `BackgroundService` to meet the 5 s update target |
| Domain project references Common.Application for `IRepository` | `IRepository` lives in SharedKernel |
| Some domain methods throw for business rules | Return `Result` |
| `DateTime.UtcNow` inside code | `TimeProvider` injected; `DateTimeOffset` UTC |
| Runtime `AddDbContext` without the per-schema history table | Set `MigrationsHistoryTable` in both the factory and the runtime registration |
| Template Api project references Infrastructure | Api references Application only |

---

## 14. Done checklist

- [ ] No reference to another module except its `Contracts`; architecture tests green
- [ ] Domain has no framework references; entity methods return `Result`
- [ ] Error codes defined in `<Module>Errors` with the right `ErrorType`
- [ ] Command/query are `sealed record`; handlers `sealed class`
- [ ] Validator exists for every command with input
- [ ] State change, audit row and outbox rows saved in **one** `SaveChangesAsync`
- [ ] No publishing, SignalR or HTTP calls from command handlers
- [ ] Event handlers idempotent
- [ ] Configuration class added; migration created and reviewed; applies on an empty database
- [ ] Endpoint maps errors through `ApiResults.Problem`; permission policy set; `.WithSummary` added
- [ ] Freshness and confidence respected wherever journey data is returned
- [ ] Happy path and one failure path tested

## 15. Keeping this skill current

When a pattern changes, a mistake is corrected, or a new constraint is agreed, update this file in the same change, including every section that mentions the old pattern.
