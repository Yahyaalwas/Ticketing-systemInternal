namespace ITS.Domain.Enums;

public enum ActivityType
{
    TicketCreated = 1,
    StatusChanged = 2,
    FieldUpdated = 3,
    CommentAdded = 4,
    CommentEdited = 5,
    CommentDeleted = 6,
    AttachmentAdded = 7,
    AttachmentDeleted = 8,
    AssigneeChanged = 9,
    PriorityChanged = 10,
    LabelAdded = 11,
    LabelRemoved = 12,
    LinkAdded = 13,
    LinkRemoved = 14,
    WatcherAdded = 15,
    WatcherRemoved = 16,
    EpicChanged = 17,
    TicketDeleted = 18,
    TicketRestored = 19
}
