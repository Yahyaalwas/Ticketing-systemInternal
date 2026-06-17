using FluentValidation;

namespace ITS.Application.Features.Attachments.Commands.AddAttachment;

public sealed class AddAttachmentCommandValidator : AbstractValidator<AddAttachmentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "text/plain", "text/csv",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/zip", "application/x-zip-compressed"
    ];

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public AddAttachmentCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("File type is not allowed.");
        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");
        RuleFor(x => x.FileContent).NotNull();
    }
}
