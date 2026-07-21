using System.Text.Json;
using UTOP.Booking.Domain.Events;
using UTOP.Booking.Infrastructure.Persistence;
using UTOP.Shared.Domain.Events;
using BookingAggregate = UTOP.Booking.Domain.Aggregates.Booking;

namespace UTOP.Booking.Infrastructure.Messaging;

/// <summary>
/// Translates domain events to integration events at the outbox boundary (LLD §10.1).
/// Internal domain types do not leak into integration events.
///
/// Most domain events carry enough data to translate on their own. Two do not:
/// BookingCreated lacks city/country/airport-code data (JourneyRoute only carries
/// Location codes, not city/country — those live on Itinerary), and BookingConfirmed
/// lacks DepartureUtc. Both are filled in from the current Booking aggregate state,
/// which is safe here because translation always happens inside the same SaveAsync
/// call, on the same in-memory aggregate, before it's persisted.
/// </summary>
public static class BookingEventTranslator
{
    public static OutboxEventEntity ToOutboxEvent(DomainEvent domainEvent, BookingAggregate booking)
    {
        var (routingKey, payload) = domainEvent switch
        {
            BookingCreated e => ("booking.created", (object)new BookingCreatedIntegrationEvent(
                EventId: e.EventId,
                CorrelationId: e.CorrelationId.Value,
                BookingId: e.BookingId.Value,
                Mode: e.Mode.ToString(),
                Category: e.Category.ToString(),
                OriginCity: booking.Itinerary.DepartureCity,
                OriginCountry: booking.Itinerary.DepartureCountry,
                OriginAirportCode: booking.Itinerary.DeparturePoint.Code,
                DestinationCity: booking.Itinerary.ArrivalCity,
                DestinationCountry: booking.Itinerary.ArrivalCountry,
                DestinationAirportCode: booking.Itinerary.ArrivalPoint.Code,
                TotalAmount: e.TotalPrice.Amount,
                Currency: e.TotalPrice.Currency.ToString(),
                Adults: booking.Passengers.Adults,
                Children: booking.Passengers.Children,
                Infants: booking.Passengers.Infants,
                OperatorId: e.OperatorId,
                GroupId: booking.GroupId,
                PilgrimageId: booking.PilgrimageId,
                DepartureUtc: booking.Itinerary.DepartureUtc,
                OccurredAt: e.OccurredAt)),

            BookingConfirmed e => ("booking.confirmed", (object)new BookingConfirmedIntegrationEvent(
                EventId: e.EventId,
                CorrelationId: e.CorrelationId.Value,
                BookingId: e.BookingId.Value,
                Category: e.Category.ToString(),
                TotalAmount: e.TotalPrice.Amount,
                Currency: e.TotalPrice.Currency.ToString(),
                Adults: e.Passengers.Adults,
                Children: e.Passengers.Children,
                Infants: e.Passengers.Infants,
                DepartureUtc: booking.Itinerary.DepartureUtc,
                OccurredAt: e.OccurredAt)),

            BookingAmended e => ("booking.amended", (object)new BookingAmendedIntegrationEvent(
                EventId: e.EventId,
                CorrelationId: e.CorrelationId.Value,
                BookingId: e.BookingId.Value,
                AmendmentVersion: e.AmendmentVersion,
                NewDepartureUtc: e.NewItinerary.DepartureUtc,
                NewArrivalUtc: e.NewItinerary.ArrivalUtc,
                NewTotalAmount: e.NewPrice.Amount,
                Currency: e.NewPrice.Currency.ToString(),
                OccurredAt: e.OccurredAt)),

            BookingCancelled e => ("booking.cancelled", (object)new BookingCancelledIntegrationEvent(
                EventId: e.EventId,
                CorrelationId: e.CorrelationId.Value,
                BookingId: e.BookingId.Value,
                Reason: e.Reason,
                CancelledAt: e.CancelledAt,
                OccurredAt: e.OccurredAt)),

            BookingEscalated e => ("booking.escalated", (object)new BookingEscalatedIntegrationEvent(
                EventId: e.EventId,
                CorrelationId: e.CorrelationId.Value,
                BookingId: e.BookingId.Value,
                Reason: e.Reason,
                OccurredAt: e.OccurredAt)),

            BookingCompleted e => ("booking.completed", (object)new BookingCompletedIntegrationEvent(
                EventId: e.EventId,
                CorrelationId: e.CorrelationId.Value,
                BookingId: e.BookingId.Value,
                OccurredAt: e.OccurredAt)),

            _ => throw new InvalidOperationException(
                $"No integration event translation defined for domain event type '{domainEvent.GetType().Name}'.")
        };

        return new OutboxEventEntity
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            EventType = routingKey,
            Payload = JsonSerializer.Serialize(payload, payload.GetType()),
            CorrelationId = domainEvent.CorrelationId.Value,
            OccurredAt = domainEvent.OccurredAt,
            PublishedAt = null,
            CreatedAt = domainEvent.OccurredAt
        };
    }
}
