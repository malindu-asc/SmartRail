using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Ticketing.Application.Abstractions;
using SmartRail.Modules.Ticketing.Domain.Bookings;

namespace SmartRail.Modules.Ticketing.Infrastructure.Persistence;

public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options) : BaseDbContext(options), ITicketingUnitOfWork
{
    public const string Schema = "ticketing";

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
        modelBuilder.UseSnakeCaseColumns("SmartRail.Modules.Ticketing");
    }

    // pg_advisory_xact_lock makes every booking of one train wait for the one before it, until its transaction ends.
    public async Task<ITransactionScope> LockJourneyAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        await Database.ExecuteSqlAsync($"select pg_advisory_xact_lock(hashtextextended({journeyId.ToString()}, 0))", cancellationToken);
        return new Scope(transaction);
    }

    private sealed class Scope(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction) : ITransactionScope
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings", t =>
        {
            t.HasCheckConstraint("ck_bookings_passengers", "adults >= 0 and children >= 0 and seniors >= 0 and adults + children + seniors between 1 and 9");
            t.HasCheckConstraint("ck_bookings_segment", "to_index > from_index");
        });

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Reference).IsRequired().HasMaxLength(20);
        builder.Property(b => b.BookingCode).IsRequired().HasMaxLength(8);
        builder.Property(b => b.UserId).HasMaxLength(64);
        builder.Property(b => b.ContactEmail).HasMaxLength(200);
        builder.Property(b => b.RouteCode).IsRequired().HasMaxLength(10);
        builder.Property(b => b.TrainNumber).IsRequired().HasMaxLength(10);
        builder.Property(b => b.FromStationCode).IsRequired().HasMaxLength(5);
        builder.Property(b => b.ToStationCode).IsRequired().HasMaxLength(5);
        builder.Property(b => b.FareTotal).HasPrecision(12, 2);
        builder.Property(b => b.Currency).IsRequired().HasMaxLength(3);
        builder.Property(b => b.Seats).HasColumnType("jsonb").HasConversion(SeatJson.Converter).Metadata.SetValueComparer(SeatJson.Comparer);
        builder.Ignore(b => b.Passengers);

        builder.HasIndex(b => b.Reference).IsUnique().HasDatabaseName("ux_bookings_reference");
        builder.HasIndex(b => b.BookingCode).IsUnique().HasDatabaseName("ux_bookings_booking_code");
        builder.HasIndex(b => b.UserId).HasDatabaseName("ix_bookings_user");
        builder.HasIndex(b => b.JourneyId).HasDatabaseName("ix_bookings_journey");
    }
}

public sealed class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.ToTable("seat_holds");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.UserId).IsRequired().HasMaxLength(64);
        builder.Property(h => h.Seats).HasColumnType("jsonb").HasConversion(SeatJson.Converter).Metadata.SetValueComparer(SeatJson.Comparer);
        builder.HasIndex(h => h.JourneyId).HasDatabaseName("ix_seat_holds_journey");
    }
}

internal static class SeatJson
{
    public static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<SeatRef>, string> Converter = new(
        seats => JsonSerializer.Serialize(seats, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<SeatRef>>(json, (JsonSerializerOptions?)null) ?? new List<SeatRef>());

    public static readonly ValueComparer<List<SeatRef>> Comparer = new(
        (a, b) => (a ?? new List<SeatRef>()).SequenceEqual(b ?? new List<SeatRef>()),
        seats => seats.Aggregate(0, (hash, s) => HashCode.Combine(hash, s)),
        seats => seats.ToList());
}
