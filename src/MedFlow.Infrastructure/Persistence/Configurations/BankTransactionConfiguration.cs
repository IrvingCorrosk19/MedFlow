using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("BankTransactions");

        builder.Property(b => b.Description).HasMaxLength(300);
        builder.Property(b => b.Reference).HasMaxLength(100);
        builder.Property(b => b.Amount).HasPrecision(18, 2);

        builder.HasOne(b => b.BankAccount)
            .WithMany(ba => ba.Transactions)
            .HasForeignKey(b => b.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.JournalEntry)
            .WithMany()
            .HasForeignKey(b => b.JournalEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(b => b.TenantId).IsRequired();
        builder.HasIndex(b => b.TenantId);
        builder.HasIndex(b => new { b.TenantId, b.TransactionDate });
        builder.HasIndex(b => new { b.BankAccountId, b.TransactionDate });
        builder.HasIndex(b => new { b.BankAccountId, b.IsReconciled });

        builder.HasOne(b => b.Tenant)
            .WithMany()
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
