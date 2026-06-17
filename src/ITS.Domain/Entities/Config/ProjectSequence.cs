namespace ITS.Domain.Entities.Config;

public class ProjectSequence
{
    private ProjectSequence() { }

    public static ProjectSequence CreateForProject(Guid projectId)
        => new() { ProjectId = projectId, CurrentNumber = 0 };

    public Guid ProjectId { get; private set; }
    public int CurrentNumber { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
}
