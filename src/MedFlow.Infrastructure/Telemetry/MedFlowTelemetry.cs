using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MedFlow.Infrastructure.Telemetry;

/// <summary>Centralized OpenTelemetry ActivitySource and Meter for MedFlow.</summary>
public static class MedFlowTelemetry
{
    public static readonly ActivitySource ActivitySource = new("MedFlow.Infrastructure", "1.0.0");
    public static readonly Meter Meter = new("MedFlow.Infrastructure", "1.0.0");

    // Counters
    public static readonly Counter<long> JournalEntriesPosted =
        Meter.CreateCounter<long>("medflow.journal_entries.posted", "entries", "Number of journal entries posted");

    public static readonly Counter<long> PaymentsRegistered =
        Meter.CreateCounter<long>("medflow.payments.registered", "payments", "Number of payments registered");

    public static readonly Counter<long> InvoicesCreated =
        Meter.CreateCounter<long>("medflow.billing_invoices.created", "invoices", "Number of billing invoices created");

    public static readonly Counter<long> PaymentsCancelled =
        Meter.CreateCounter<long>("medflow.payments.cancelled", "payments", "Number of payments cancelled");
}
