# Domain Models
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0  
**Status:** LOCKED — Ready for LLD  
**Phase:** Phase 3 — System Architecture & Design  
**Classification:** Project Internal — Binding Architectural Specification  

---

## Domain Modeling Principles

All domain models follow DDD (Domain-Driven Design) principles:
- **Aggregates** enforce consistency boundaries and business invariants
- **Entities** have identity and lifecycle; mutable
- **Value Objects** are immutable; compared by value, not identity
- **Domain Events** represent significant state changes; published after successful persistence
- **Domain Services** contain logic that spans multiple aggregates or doesn't naturally belong to one
- **Repositories** are interfaces (defined in Domain); implementations in Infrastructure
- **No infrastructure dependencies** in domain layer (no EF Core, no RabbitMQ, no HTTP)

---

## 1. Shared Kernel (UTOP.Shared)

Value objects and base classes used across multiple bounded contexts.

### 1.1 Value Objects

```csharp
// Money.cs
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);
    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new CurrencyMismatchException(Currency, other.Currency);
        return new Money(Amount + other.Amount, Currency);
    }
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);
    public Money Divide(int divisor)
    {
        if (divisor == 0) throw new DivideByZeroException("Cannot divide money by zero");
        return new Money(Math.Round(Amount / divisor, 2, MidpointRounding.AwayFromZero), Currency);
    }
    public override string ToString() => $"{Amount:F2} {Currency}";
}

// DateRange.cs
public sealed record DateRange(DateOnly Start, DateOnly End)
{
    public DateRange
    {
        if (End < Start) throw new InvalidDateRangeException(Start, End);
    }
    public int Nights => End.DayNumber - Start.DayNumber;
    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;
    public bool Contains(DateOnly date) => date >= Start && date <= End;
}

// Location.cs
public sealed record Location(string City, string Country, string? AirportCode = null, decimal? Latitude = null, decimal? Longitude = null)
{
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    public override string ToString() => AirportCode != null ? $"{City} ({AirportCode}), {Country}" : $"{City}, {Country}";
}

// CorrelationId.cs
public sealed record CorrelationId(string Value)
{
    public static CorrelationId New() => new($"utop-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}");
    public override string ToString() => Value;
}

// PassengerCount.cs
public sealed record PassengerCount(int Adults, int Children = 0, int Infants = 0)
{
    public int Total => Adults + Children + Infants;
    public PassengerCount
    {
        if (Adults < 1) throw new InvalidPassengerCountException("At least one adult required");
    }
}
```

### 1.2 Base Classes

```csharp
// AggregateRoot.cs
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
}

// DomainEvent.cs
public abstract record DomainEvent(
    string EventId,
    string EventType,
    string CorrelationId,
    string AggregateId,
    string AggregateType,
    DateTime OccurredAt
);

// Entity.cs
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public override bool Equals(object? obj) => obj is Entity other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

// DomainException.cs
public abstract class DomainException : Exception
{
    public string Code { get; }
    protected DomainException(string code, string message) : base(message) => Code = code;
}
```

---

## 2. Booking Context (UTOP.Booking)

### 2.1 Aggregates

```csharp
// Booking.cs — Aggregate Root
public class Booking : AggregateRoot
{
    public BookingId BookingId { get; private set; }
    public TravelMode Mode { get; private set; }
    public JourneyRoute Route { get; private set; }
    public PassengerCount Passengers { get; private set; }
    public BookingStatus Status { get; private set; }
    public Money TotalPrice { get; private set; }
    public TravelCategory Category { get; private set; }
    public string OperatorId { get; private set; }
    public Itinerary Itinerary { get; private set; }
    private readonly List<Passenger> _passengers = new();
    public IReadOnlyList<Passenger> PassengerList => _passengers.AsReadOnly();
    public string? GroupId { get; private set; }
    public string? PilgrimageId { get; private set; }

    private Booking() { } // EF Core

    public static Booking Create(
        TravelMode mode,
        JourneyRoute route,
        PassengerCount passengers,
        TravelCategory category,
        string operatorId,
        Money price,
        Itinerary itinerary,
        string correlationId)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingId = BookingId.Generate(mode),
            Mode = mode,
            Route = route,
            Passengers = passengers,
            Status = BookingStatus.Draft,
            TotalPrice = price,
            Category = category,
            OperatorId = operatorId,
            Itinerary = itinerary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        booking.AddDomainEvent(new BookingCreated(
            Guid.NewGuid().ToString(),
            correlationId,
            booking.BookingId.Value,
            mode.ToString(),
            route,
            price,
            DateTime.UtcNow));

        return booking;
    }

    public void Confirm(string correlationId)
    {
        if (Status != BookingStatus.Draft)
            throw new InvalidBookingStateException(BookingId.Value, Status, BookingStatus.Confirmed);

        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookingConfirmed(
            Guid.NewGuid().ToString(),
            correlationId,
            BookingId.Value,
            Category.ToString(),
            DateTime.UtcNow));
    }

    public void Cancel(string reason, string correlationId)
    {
        if (Status == BookingStatus.Cancelled)
            throw new InvalidBookingStateException(BookingId.Value, Status, BookingStatus.Cancelled);
        if (Status == BookingStatus.Completed)
            throw new CannotCancelCompletedBookingException(BookingId.Value);

        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookingCancelled(
            Guid.NewGuid().ToString(),
            correlationId,
            BookingId.Value,
            reason,
            DateTime.UtcNow));
    }

    public void Amend(JourneyRoute newRoute, Money newPrice, string correlationId)
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidBookingStateException(BookingId.Value, Status, BookingStatus.Confirmed);

        var oldRoute = Route;
        Route = newRoute;
        TotalPrice = newPrice;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookingAmended(
            Guid.NewGuid().ToString(),
            correlationId,
            BookingId.Value,
            oldRoute,
            newRoute,
            newPrice,
            DateTime.UtcNow));
    }

    public void AssignToGroup(string groupId)
    {
        if (Category != TravelCategory.Group && Category != TravelCategory.Pilgrimage)
            throw new InvalidCategoryForGroupAssignmentException(BookingId.Value, Category);
        GroupId = groupId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToPilgrimage(string pilgrimageId)
    {
        if (Category != TravelCategory.Pilgrimage)
            throw new InvalidCategoryForPilgrimageException(BookingId.Value, Category);
        PilgrimageId = pilgrimageId;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 2.2 Entities

```csharp
// Itinerary.cs
public class Itinerary : Entity
{
    public string BookingId { get; private set; }
    public IReadOnlyList<ItineraryLeg> Legs { get; private set; }
    public DateTime DepartureTime { get; private set; }
    public DateTime ArrivalTime { get; private set; }
    public int TotalDurationMinutes => Legs.Sum(l => l.DurationMinutes);
    public bool IsMultiLeg => Legs.Count > 1;
}

// ItineraryLeg.cs
public class ItineraryLeg : Entity
{
    public int LegNumber { get; private set; }
    public Location Origin { get; private set; }
    public Location Destination { get; private set; }
    public DateTime DepartureTime { get; private set; }
    public DateTime ArrivalTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public TravelMode Mode { get; private set; }
    public string? VehicleReference { get; private set; }
}

// Passenger.cs
public class Passenger : Entity
{
    public string BookingId { get; private set; }
    public PersonName Name { get; private set; }
    public PassengerType Type { get; private set; } // Adult, Child, Infant
    public string? PassportNumber { get; private set; }    // Encrypted at rest
    public DateOnly? DateOfBirth { get; private set; }
    public string? DietaryRequirement { get; private set; }
    public string? AccessibilityRequirement { get; private set; }
}
```

### 2.3 Value Objects

```csharp
// BookingId.cs
public sealed record BookingId(string Value)
{
    public static BookingId Generate(TravelMode mode)
    {
        var modeCode = mode switch
        {
            TravelMode.Bus => "BUS",
            TravelMode.Train => "TRN",
            TravelMode.Plane => "AIR",
            TravelMode.Cruise => "CRU",
            _ => "GEN"
        };
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var hash = Guid.NewGuid().ToString("N")[..5].ToUpper();
        return new BookingId($"UTOP-{modeCode}-{timestamp}-{hash}");
    }
}

// JourneyRoute.cs
public sealed record JourneyRoute(Location Origin, Location Destination)
{
    public override string ToString() => $"{Origin} → {Destination}";
}

// PersonName.cs
public sealed record PersonName(string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}";
    public PersonName
    {
        if (string.IsNullOrWhiteSpace(FirstName)) throw new InvalidPersonNameException("First name required");
        if (string.IsNullOrWhiteSpace(LastName)) throw new InvalidPersonNameException("Last name required");
    }
}
```

### 2.4 Enumerations

```csharp
public enum TravelMode { Bus, Train, Plane, Cruise }
public enum TravelCategory { Personal, Leisure, Religious, Group }
public enum PassengerType { Adult, Child, Infant }
public enum BookingStatus
{
    Draft,
    PendingValidation,
    PendingPayment,
    Confirmed,
    Allocated,
    InTransit,
    Completed,
    Cancelled,
    Refunded,
    Escalated
}
```

### 2.5 Domain Events

```csharp
public record BookingCreated(
    string EventId, string CorrelationId, string BookingId,
    string Mode, JourneyRoute Route, Money Price, DateTime OccurredAt
) : DomainEvent(EventId, nameof(BookingCreated), CorrelationId, BookingId, "Booking", OccurredAt);

public record BookingConfirmed(
    string EventId, string CorrelationId, string BookingId,
    string Category, DateTime OccurredAt
) : DomainEvent(EventId, nameof(BookingConfirmed), CorrelationId, BookingId, "Booking", OccurredAt);

public record BookingCancelled(
    string EventId, string CorrelationId, string BookingId,
    string CancellationReason, DateTime OccurredAt
) : DomainEvent(EventId, nameof(BookingCancelled), CorrelationId, BookingId, "Booking", OccurredAt);

public record BookingAmended(
    string EventId, string CorrelationId, string BookingId,
    JourneyRoute OldRoute, JourneyRoute NewRoute, Money NewPrice, DateTime OccurredAt
) : DomainEvent(EventId, nameof(BookingAmended), CorrelationId, BookingId, "Booking", OccurredAt);
```

### 2.6 Repository Interface

```csharp
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(string bookingId, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByOperatorIdAsync(string operatorId, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByGroupIdAsync(string groupId, CancellationToken ct = default);
    Task SaveAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
}
```

---

## 3. Resource Allocation Context (UTOP.ResourceAllocation)

### 3.1 Aggregates

```csharp
// AllocationDecision.cs — Aggregate Root
public class AllocationDecision : AggregateRoot
{
    public string BookingId { get; private set; }
    public string ResourceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public AllocationStatus Status { get; private set; }
    public AllocationStrategy StrategyUsed { get; private set; }
    public int PriorityScore { get; private set; }
    public string DecisionRationale { get; private set; }
    public string? OverriddenByManagerId { get; private set; }
    public string? OverrideJustification { get; private set; }
    public DateTime AllocatedAt { get; private set; }

    public static AllocationDecision Allocate(
        string bookingId, string resourceId, ResourceType resourceType,
        AllocationStrategy strategy, int priorityScore, string rationale, string correlationId)
    {
        var decision = new AllocationDecision
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            ResourceId = resourceId,
            ResourceType = resourceType,
            Status = AllocationStatus.Confirmed,
            StrategyUsed = strategy,
            PriorityScore = priorityScore,
            DecisionRationale = rationale,
            AllocatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decision.AddDomainEvent(new ResourceAllocated(
            Guid.NewGuid().ToString(), correlationId,
            bookingId, resourceId, strategy.ToString(), priorityScore, DateTime.UtcNow));

        return decision;
    }

    public void OverrideByManager(string managerId, string justification, string newResourceId, string correlationId)
    {
        OverriddenByManagerId = managerId;
        OverrideJustification = justification;
        ResourceId = newResourceId;
        Status = AllocationStatus.ManuallyOverridden;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AllocationOverridden(
            Guid.NewGuid().ToString(), correlationId,
            BookingId, managerId, justification, DateTime.UtcNow));
    }

    public void Escalate(string reason, string correlationId)
    {
        Status = AllocationStatus.EscalatedToManager;
        DecisionRationale = reason;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ResourceConflictDetected(
            Guid.NewGuid().ToString(), correlationId,
            BookingId, reason, DateTime.UtcNow));
    }
}

// Resource.cs — Aggregate Root
public class Resource : AggregateRoot
{
    public string ResourceCode { get; private set; }
    public ResourceType Type { get; private set; }
    public string Name { get; private set; }
    public int Capacity { get; private set; }
    public ResourceStatus Status { get; private set; }
    public bool IsAccessible { get; private set; }   // Wheelchair accessible
    private readonly List<ResourceAvailability> _availability = new();
    public IReadOnlyList<ResourceAvailability> Availability => _availability.AsReadOnly();

    public bool IsAvailableFor(DateRange range, int requiredCapacity)
        => Status == ResourceStatus.Active &&
           Capacity >= requiredCapacity &&
           !_availability.Any(a => a.DateRange.Overlaps(range) && a.IsBlocked);

    public void Block(DateRange range, string reason)
    {
        _availability.Add(new ResourceAvailability(Id, range, isBlocked: true, reason));
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 3.2 Value Objects

```csharp
public enum ResourceType { Bus, Train, Aircraft, CruiseShip, Hotel, Guide, Driver, Coordinator }
public enum AllocationStatus { Pending, Confirmed, ManuallyOverridden, EscalatedToManager, Released }
public enum AllocationStrategy { FirstComeFirstServed, HighValueFirst, VIPFirst, GroupFirst, ReligiousComplianceFirst, Custom }
public enum ResourceStatus { Active, Maintenance, Decommissioned }
```

### 3.3 Domain Services

```csharp
// AllocationPolicyService.cs
public class AllocationPolicyService
{
    public AllocationStrategy DetermineStrategy(Booking booking, IEnumerable<AllocationPolicy> policies)
    {
        // Apply policy rules in priority order
        if (booking.Category == TravelCategory.Religious) return AllocationStrategy.ReligiousComplianceFirst;
        if (IsVipBooking(booking, policies)) return AllocationStrategy.VIPFirst;
        if (booking.Category == TravelCategory.Group) return AllocationStrategy.GroupFirst;
        if (IsHighValueBooking(booking, policies)) return AllocationStrategy.HighValueFirst;
        return AllocationStrategy.FirstComeFirstServed;
    }

    public int CalculatePriorityScore(Booking booking, AllocationStrategy strategy)
    {
        return strategy switch
        {
            AllocationStrategy.ReligiousComplianceFirst => 100,
            AllocationStrategy.VIPFirst => 90,
            AllocationStrategy.GroupFirst => 80,
            AllocationStrategy.HighValueFirst => booking.TotalPrice.Amount > 50000 ? 70 : 60,
            AllocationStrategy.FirstComeFirstServed => 50,
            _ => 40
        };
    }

    public string BuildRationale(Booking booking, AllocationStrategy strategy, Resource resource)
        => $"Strategy: {strategy}. Booking category: {booking.Category}. " +
           $"Resource: {resource.Name} (capacity: {resource.Capacity}). " +
           $"Priority score: {CalculatePriorityScore(booking, strategy)}.";
}
```

---

## 4. Pilgrimage Context (UTOP.Pilgrimage)

### 4.1 Aggregates

```csharp
// PilgrimageGroup.cs — Aggregate Root
public class PilgrimageGroup : AggregateRoot
{
    public string PilgrimageId { get; private set; }
    public PilgrimageType Type { get; private set; }       // Hajj, Umrah, KumbhMela, etc.
    public Religion Religion { get; private set; }
    public DateRange TravelDates { get; private set; }
    public string GuideId { get; private set; }
    public PilgrimageStatus Status { get; private set; }
    public IReadOnlyList<SacredSiteVisit> SacredSiteVisits { get; private set; }
    public IReadOnlyList<PrayerScheduleEntry> PrayerSchedule { get; private set; }
    public GroupCohesionLevel CohesionLevel { get; private set; }
    private readonly List<string> _pilgrimBookingIds = new();
    public IReadOnlyList<string> PilgrimBookingIds => _pilgrimBookingIds.AsReadOnly();
    public ComplianceCheckResult? LastComplianceCheck { get; private set; }

    public void RunComplianceCheck(
        PrayerSchedule schedule,
        IEnumerable<SacredSiteConstraint> siteConstraints,
        string correlationId)
    {
        var violations = new List<ComplianceViolation>();

        // Check prayer schedule conflicts
        foreach (var leg in GetTransportLegs())
        {
            foreach (var prayer in schedule.Prayers)
            {
                if (leg.DepartureTime <= prayer.Time && leg.ArrivalTime >= prayer.Time)
                    violations.Add(new ComplianceViolation(
                        ComplianceViolationType.PrayerScheduleConflict,
                        $"Leg {leg.LegNumber} overlaps with {prayer.Name} prayer at {prayer.Time:HH:mm}"));
            }
        }

        // Check sacred site access
        foreach (var visit in SacredSiteVisits)
        {
            var constraint = siteConstraints.FirstOrDefault(c => c.SiteId == visit.SiteId);
            if (constraint != null && !constraint.IsAccessibleOn(visit.PlannedDate))
                violations.Add(new ComplianceViolation(
                    ComplianceViolationType.SacredSiteAccessDenied,
                    $"Sacred site {visit.SiteName} not accessible on {visit.PlannedDate}"));
        }

        // Check guide assignment
        if (string.IsNullOrEmpty(GuideId))
            violations.Add(new ComplianceViolation(
                ComplianceViolationType.GuideNotAssigned,
                "No qualified guide assigned to pilgrimage group"));

        LastComplianceCheck = new ComplianceCheckResult(
            DateTime.UtcNow,
            violations.Count == 0,
            violations);

        AddDomainEvent(new PilgrimageComplianceChecked(
            Guid.NewGuid().ToString(), correlationId,
            PilgrimageId, violations.Count == 0, violations.Count, DateTime.UtcNow));
    }

    public void EnforceGroupCohesion()
    {
        CohesionLevel = GroupCohesionLevel.Strict; // All pilgrims must stay together
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 4.2 Entities

```csharp
// SacredSiteVisit.cs
public class SacredSiteVisit : Entity
{
    public string SiteId { get; private set; }
    public string SiteName { get; private set; }
    public DateOnly PlannedDate { get; private set; }
    public TimeOnly PlannedArrival { get; private set; }
    public int PlannedDurationHours { get; private set; }
    public bool SpecialAccessRequired { get; private set; }
    public string? AccessEligibilityCriteria { get; private set; }
}

// PrayerScheduleEntry.cs
public class PrayerScheduleEntry : Entity
{
    public string PrayerName { get; private set; }  // Fajr, Dhuhr, Asr, Maghrib, Isha
    public DateTime PrayerTime { get; private set; }
    public Location Location { get; private set; }
    public string? NearestPrayerFacility { get; private set; }
    public bool HasPrayerFacilityNearby { get; private set; }
}

// ComplianceViolation.cs
public class ComplianceViolation : Entity
{
    public ComplianceViolationType Type { get; private set; }
    public string Description { get; private set; }
    public ComplianceSeverity Severity { get; private set; }
    public string? SuggestedRemedy { get; private set; }
}
```

### 4.3 Value Objects

```csharp
public enum PilgrimageType { Hajj, Umrah, KumbhMela, ChardhamYatra, CaminoDeSantiago, Jerusalem, Other }
public enum Religion { Islam, Hinduism, Christianity, Buddhism, Sikhism, Judaism, Other }
public enum PilgrimageStatus { Planning, GuideAssigned, Booked, InProgress, Completed, Cancelled }
public enum GroupCohesionLevel { Flexible, Recommended, Strict }
public enum ComplianceViolationType { PrayerScheduleConflict, SacredSiteAccessDenied, GuideNotAssigned, GroupCohesionViolation }
public enum ComplianceSeverity { Info, Warning, Critical }

public sealed record ComplianceCheckResult(
    DateTime CheckedAt,
    bool Passed,
    IReadOnlyList<ComplianceViolation> Violations);
```

---

## 5. Group Management Context (UTOP.GroupManagement)

### 5.1 Aggregates

```csharp
// Group.cs — Aggregate Root
public class Group : AggregateRoot
{
    public string GroupId { get; private set; }
    public string GroupName { get; private set; }
    public string CoordinatorId { get; private set; }
    public GroupStatus Status { get; private set; }
    public TravelCategory Category { get; private set; }
    public DateRange TravelDates { get; private set; }
    private readonly List<GroupMember> _members = new();
    public IReadOnlyList<GroupMember> Members => _members.AsReadOnly();
    public int MemberCount => _members.Count(m => m.Status == MemberStatus.Active);

    public void AddMember(string userId, string name, MemberRole role, string correlationId)
    {
        if (_members.Any(m => m.UserId == userId && m.Status == MemberStatus.Active))
            throw new DuplicateGroupMemberException(GroupId, userId);

        var member = GroupMember.Create(GroupId, userId, name, role);
        _members.Add(member);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new GroupMemberJoined(
            Guid.NewGuid().ToString(), correlationId,
            GroupId, userId, role.ToString(), MemberCount, DateTime.UtcNow));
    }

    public void RemoveMember(string userId, string reason, string correlationId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active)
            ?? throw new GroupMemberNotFoundException(GroupId, userId);

        if (member.Role == MemberRole.Coordinator)
            throw new CannotRemoveCoordinatorException(GroupId);

        member.Deactivate(reason);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new GroupMemberLeft(
            Guid.NewGuid().ToString(), correlationId,
            GroupId, userId, reason, MemberCount, DateTime.UtcNow));
    }
}
```

### 5.2 Entities

```csharp
// GroupMember.cs
public class GroupMember : Entity
{
    public string GroupId { get; private set; }
    public string UserId { get; private set; }
    public string Name { get; private set; }
    public MemberRole Role { get; private set; }
    public MemberStatus Status { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public string? BookingId { get; private set; }
    public MemberPreferences Preferences { get; private set; }

    public static GroupMember Create(string groupId, string userId, string name, MemberRole role)
        => new() { GroupId = groupId, UserId = userId, Name = name, Role = role, Status = MemberStatus.Active, JoinedAt = DateTime.UtcNow };

    public void Deactivate(string reason)
    {
        Status = MemberStatus.Left;
    }
}
```

### 5.3 Value Objects

```csharp
public enum MemberRole { Coordinator, Member, Guest }
public enum MemberStatus { Invited, Active, Left, Removed }
public enum GroupStatus { Forming, Booking, Confirmed, InProgress, Completed, Cancelled }

public sealed record MemberPreferences(
    string? AccommodationType,     // Single, Shared
    string? DietaryRequirement,    // Vegetarian, Halal, Kosher, etc.
    bool AccessibilityRequired,
    string? SpecialRequests);
```

---

## 6. Cost Splitting Context (UTOP.CostSplitting)

### 6.1 Aggregates

```csharp
// CostLedger.cs — Aggregate Root
public class CostLedger : AggregateRoot
{
    public string LedgerId { get; private set; }
    public string GroupId { get; private set; }
    public string BookingId { get; private set; }
    public Money TotalCost { get; private set; }
    public CostSplitFormula Formula { get; private set; }
    public LedgerStatus Status { get; private set; }
    public int MemberCount { get; private set; }
    private readonly List<CostShare> _shares = new();
    public IReadOnlyList<CostShare> Shares => _shares.AsReadOnly();
    private readonly List<CostLedgerEntry> _entries = new();
    public IReadOnlyList<CostLedgerEntry> Entries => _entries.AsReadOnly();

    public void CalculateShares(IEnumerable<GroupMemberCost> memberCosts, string correlationId)
    {
        _shares.Clear();

        var sharedCost = memberCosts.Where(c => c.IsShared).Sum(c => c.Amount.Amount);
        var perMemberShare = Math.Round(sharedCost / MemberCount, 2, MidpointRounding.AwayFromZero);

        foreach (var memberCost in memberCosts)
        {
            var individualCost = memberCost.IsShared ? perMemberShare : memberCost.Amount.Amount;
            var total = individualCost + memberCost.PersonalExpenses.Amount;
            _shares.Add(CostShare.Create(LedgerId, memberCost.MemberId, new Money(total, TotalCost.Currency)));
        }

        // Adjust rounding differences to first member
        var calculatedTotal = _shares.Sum(s => s.Amount.Amount);
        var roundingDiff = TotalCost.Amount - calculatedTotal;
        if (roundingDiff != 0)
            _shares.First().AdjustForRounding(new Money(roundingDiff, TotalCost.Currency));

        AddDomainEvent(new CostShareCalculated(
            Guid.NewGuid().ToString(), correlationId,
            GroupId, MemberCount, TotalCost, DateTime.UtcNow));
    }

    public void RecalculateForMemberChange(int newMemberCount, string reason, string correlationId)
    {
        MemberCount = newMemberCount;
        CalculateShares(_entries.Select(e => new GroupMemberCost(e.MemberId, e.Amount, e.IsShared, Money.Zero(TotalCost.Currency))), correlationId);

        AddDomainEvent(new CostShareRecalculated(
            Guid.NewGuid().ToString(), correlationId,
            GroupId, reason, newMemberCount, TotalCost, DateTime.UtcNow));
    }

    public Money CalculateRefundForDeparture(string memberId, DateOnly departureDate)
    {
        var share = _shares.FirstOrDefault(s => s.MemberId == memberId)
            ?? throw new CostShareNotFoundException(LedgerId, memberId);
        var cancellationPolicy = DetermineCancellationPolicy(departureDate);
        return new Money(share.Amount.Amount * cancellationPolicy.RefundPercentage / 100, TotalCost.Currency);
    }
}
```

### 6.2 Entities

```csharp
// CostShare.cs
public class CostShare : Entity
{
    public string LedgerId { get; private set; }
    public string MemberId { get; private set; }
    public Money Amount { get; private set; }
    public Money PaidAmount { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? PaymentDeadline { get; private set; }

    public Money OutstandingBalance => new(Amount.Amount - PaidAmount.Amount, Amount.Currency);
    public bool IsFullyPaid => PaidAmount.Amount >= Amount.Amount;

    public void RecordPayment(Money payment)
    {
        PaidAmount = PaidAmount.Add(payment);
        PaymentStatus = IsFullyPaid ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
    }

    internal void AdjustForRounding(Money adjustment)
    {
        Amount = Amount.Add(adjustment);
    }
}
```

### 6.3 Value Objects

```csharp
public enum CostSplitFormula { EqualSplit, PerPerson, Weighted, Custom }
public enum PaymentStatus { Pending, PartiallyPaid, Paid, Overdue, Refunded, Disputed }
public enum LedgerStatus { Draft, Active, Settled, Disputed, Closed }

public sealed record GroupMemberCost(
    string MemberId,
    Money Amount,
    bool IsShared,
    Money PersonalExpenses);
```

---

## 7. Notification Context (UTOP.Notifications)

### 7.1 Aggregates

```csharp
// Notification.cs — Aggregate Root
public class Notification : AggregateRoot
{
    public string NotificationId { get; private set; }
    public string RecipientId { get; private set; }
    public string RecipientLocale { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationEvent TriggerEvent { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string TemplateId { get; private set; }
    public Dictionary<string, string> TemplateVariables { get; private set; }
    public string? RenderedContent { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;
    public string? FailureReason { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string CorrelationId { get; private set; }

    public void MarkSent(string correlationId)
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NotificationSent(Guid.NewGuid().ToString(), correlationId, NotificationId, Channel.ToString(), RecipientId, DateTime.UtcNow));
    }

    public void MarkFailed(string reason, string correlationId)
    {
        RetryCount++;
        FailureReason = reason;
        Status = RetryCount >= MaxRetries ? NotificationStatus.PermanentlyFailed : NotificationStatus.RetryPending;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NotificationFailed(Guid.NewGuid().ToString(), correlationId, NotificationId, Channel.ToString(), reason, RetryCount, DateTime.UtcNow));
    }

    public bool CanRetry => RetryCount < MaxRetries && Status == NotificationStatus.RetryPending;
}
```

### 7.2 Value Objects

```csharp
public enum NotificationChannel { Email, SMS, Push, InApp }
public enum NotificationStatus { Pending, Sent, RetryPending, PermanentlyFailed, Cancelled }
public enum NotificationEvent
{
    BookingConfirmed, BookingCancelled, BookingAmended,
    ResourceAllocated, ResourceConflict,
    GroupMemberJoined, GroupMemberLeft,
    CostShareCalculated, PaymentReminder, PaymentConfirmed,
    PilgrimageComplianceChecked, PilgrimageConfirmed,
    SystemAlert, KnowledgeModuleAvailable
}
```

---

## 8. Identity Context (UTOP.Identity)

### 8.1 Aggregates

```csharp
// User.cs — Aggregate Root
public class User : AggregateRoot
{
    public string UserId { get; private set; }
    public string Email { get; private set; }      // Unique; used for login
    public string PasswordHash { get; private set; }   // Bcrypt hash
    public UserRole Role { get; private set; }
    public string PreferredLocale { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public bool IsLocked => FailedLoginAttempts >= 5;

    public void RecordSuccessfulLogin(string ipAddress, string sessionId, string correlationId)
    {
        FailedLoginAttempts = 0;
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedIn(Guid.NewGuid().ToString(), correlationId, UserId, Role.ToString(), sessionId, ipAddress, DateTime.UtcNow));
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (IsLocked) Status = UserStatus.TemporarilyLocked;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole newRole, string changedByAdminId, string correlationId)
    {
        if (Role == newRole) return;
        var oldRole = Role;
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserRoleChanged(Guid.NewGuid().ToString(), correlationId, UserId, oldRole.ToString(), newRole.ToString(), changedByAdminId, DateTime.UtcNow));
    }
}
```

### 8.2 Value Objects

```csharp
public enum UserRole { Operator, Manager, Analyst, Administrator, IntegrationEngineer }
public enum UserStatus { Active, TemporarilyLocked, Suspended, Deactivated }
```

---

## 9. Localization Context (UTOP.Localization)

### 9.1 Aggregates

```csharp
// LocaleConfiguration.cs — Aggregate Root
public class LocaleConfiguration : AggregateRoot
{
    public string LocaleCode { get; private set; }   // en-US, ar-SA, hi-IN, fr-FR
    public string DisplayName { get; private set; }
    public bool IsRightToLeft { get; private set; }
    public string CurrencyCode { get; private set; }
    public string DateFormat { get; private set; }
    public string TimeFormat { get; private set; }  // 12h or 24h
    public bool IsActive { get; private set; }
    private readonly Dictionary<string, TranslationEntry> _translations = new();

    public string Translate(string key, string fallbackLocale = "en-US")
    {
        if (_translations.TryGetValue(key, out var entry) && !string.IsNullOrEmpty(entry.Value))
            return entry.Value;
        // Log missing translation
        return key; // Return key as fallback (visible; triggers admin attention)
    }

    public void UpdateTranslation(string key, string value, string updatedByAdminId)
    {
        _translations[key] = new TranslationEntry(key, value, DateTime.UtcNow, updatedByAdminId);
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

## 10. Analytics Context (UTOP.Analytics)

### 10.1 Read Models (CQRS Projections)

```csharp
// BookingMetricProjection.cs — Read model (not aggregate)
public class BookingMetricProjection
{
    public DateTime Date { get; set; }
    public string Mode { get; set; }
    public string Category { get; set; }
    public string OriginCity { get; set; }
    public string DestinationCity { get; set; }
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public string Currency { get; set; }
    public string OperatorId { get; set; }
}

// ResourceUtilizationProjection.cs
public class ResourceUtilizationProjection
{
    public string ResourceId { get; set; }
    public string ResourceType { get; set; }
    public DateTime Date { get; set; }
    public int TotalCapacity { get; set; }
    public int UsedCapacity { get; set; }
    public decimal UtilizationRate => TotalCapacity > 0 ? (decimal)UsedCapacity / TotalCapacity * 100 : 0;
    public decimal Revenue { get; set; }
}
```

---

## 11. AI Recommendation Context (UTOP.AIRecommendation)

### 11.1 Aggregates

```csharp
// Recommendation.cs — Aggregate Root
public class Recommendation : AggregateRoot
{
    public string RecommendationId { get; private set; }
    public RecommendationType Type { get; private set; }
    public string ModelName { get; private set; }
    public string ModelVersion { get; private set; }
    public decimal ConfidenceScore { get; private set; }    // 0.0 - 1.0
    public string RecommendationValue { get; private set; } // JSON
    public string Rationale { get; private set; }
    public RecommendationStatus Status { get; private set; }
    public string? ReviewedByManagerId { get; private set; }
    public RecommendationDecision? ManagerDecision { get; private set; }
    public string InputContext { get; private set; }   // JSON snapshot of inputs

    public void Accept(string managerId, string correlationId)
    {
        ReviewedByManagerId = managerId;
        ManagerDecision = RecommendationDecision.Accepted;
        Status = RecommendationStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new RecommendationAccepted(Guid.NewGuid().ToString(), correlationId, RecommendationId, managerId, DateTime.UtcNow));
    }

    public void Reject(string managerId, string reason, string correlationId)
    {
        ReviewedByManagerId = managerId;
        ManagerDecision = RecommendationDecision.Rejected;
        Status = RecommendationStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 11.2 Value Objects

```csharp
public enum RecommendationType { Pricing, ResourceAllocation, DemandForecast, GroupOptimization }
public enum RecommendationStatus { Generated, Accepted, Rejected, Expired }
public enum RecommendationDecision { Accepted, Modified, Rejected }
```

---

## 12. Domain Model Summary

| Bounded Context | Aggregate Root(s) | Key Entities | Key Events |
|-----------------|-------------------|--------------|------------|
| Booking | Booking | Itinerary, Passenger | BookingCreated, BookingConfirmed, BookingCancelled |
| Accommodation | AccommodationBooking | Room, AncillaryService | AccommodationBooked, AccommodationCancelled |
| ResourceAllocation | Resource, AllocationDecision | ResourceAvailability | ResourceAllocated, ResourceConflictDetected |
| TravelCategory | CategoryConfiguration | CategoryRule | CategoryRuleUpdated |
| Pilgrimage | PilgrimageGroup | SacredSiteVisit, PrayerScheduleEntry | PilgrimageConfirmed, ComplianceChecked |
| GroupManagement | Group | GroupMember | GroupCreated, MemberJoined, MemberLeft |
| CostSplitting | CostLedger | CostShare, CostLedgerEntry | CostShareCalculated, CostShareRecalculated |
| Notifications | Notification | NotificationTemplate | NotificationSent, NotificationFailed |
| KnowledgeBase | KnowledgeModule | LearningStep, CompletionRecord | ModuleViewed, ModuleCompleted |
| Analytics | (Read Models only) | BookingMetricProjection | (Consumes events) |
| AIRecommendation | Recommendation | ModelOutput | RecommendationGenerated, RecommendationAccepted |
| Identity | User | Session | UserLoggedIn, UserLoggedOut, RoleChanged |
| Localization | LocaleConfiguration | TranslationEntry | TranslationUpdated |

---

**End of Domain Models**

**Status:** LOCKED — Ready for Low-Level Design
