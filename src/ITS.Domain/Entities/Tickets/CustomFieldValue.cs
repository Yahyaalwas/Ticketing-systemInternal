using ITS.Domain.Common;

namespace ITS.Domain.Entities.Tickets;

public class CustomFieldValue : BaseEntity<long>
{
    private CustomFieldValue() { }

    public static CustomFieldValue CreateText(Guid ticketId, int customFieldId, string value)
        => new() { TicketId = ticketId, CustomFieldId = customFieldId, ValueText = value };

    public static CustomFieldValue CreateNumber(Guid ticketId, int customFieldId, decimal value)
        => new() { TicketId = ticketId, CustomFieldId = customFieldId, ValueNumber = value };

    public static CustomFieldValue CreateDate(Guid ticketId, int customFieldId, DateTime value)
        => new() { TicketId = ticketId, CustomFieldId = customFieldId, ValueDate = value };

    public static CustomFieldValue CreateUser(Guid ticketId, int customFieldId, Guid userId)
        => new() { TicketId = ticketId, CustomFieldId = customFieldId, ValueUserId = userId };

    public Guid TicketId { get; private set; }
    public int CustomFieldId { get; private set; }
    public string? ValueText { get; private set; }
    public decimal? ValueNumber { get; private set; }
    public DateTime? ValueDate { get; private set; }
    public Guid? ValueUserId { get; private set; }

    public void UpdateText(string value) => ValueText = value;
    public void UpdateNumber(decimal value) => ValueNumber = value;
    public void UpdateDate(DateTime value) => ValueDate = value;
    public void UpdateUser(Guid userId) => ValueUserId = userId;
    public void Clear() { ValueText = null; ValueNumber = null; ValueDate = null; ValueUserId = null; }
}
