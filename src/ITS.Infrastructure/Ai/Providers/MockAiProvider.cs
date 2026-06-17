using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;

namespace ITS.Infrastructure.Ai.Providers;

/// <summary>Development/testing provider — returns realistic placeholder responses without calling any external API.</summary>
public sealed class MockAiProvider : IAiProvider
{
    public string ProviderName => "mock";
    public bool SupportsEmbeddings => true;

    public Task<AiTextResponse> CompleteAsync(AiTextRequest request, CancellationToken ct = default)
    {
        var content = GenerateMockResponse(request.SystemPrompt, request.UserPrompt);
        return Task.FromResult(new AiTextResponse(content, 100, 200, ProviderName, TimeSpan.FromMilliseconds(50)));
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        // Deterministic pseudo-embedding based on text hash
        var rng = new Random(text.GetHashCode());
        return Task.FromResult(Enumerable.Range(0, 384).Select(_ => (float)rng.NextDouble()).ToArray());
    }

    private static string GenerateMockResponse(string systemPrompt, string userPrompt)
    {
        var lowerSystem = systemPrompt.ToLowerInvariant();

        if (lowerSystem.Contains("summarize") || lowerSystem.Contains("executivesummary"))
            return """{"executiveSummary":"This ticket addresses a critical system issue requiring immediate attention from the engineering team. The problem impacts multiple users and has been escalating over the past 48 hours. Resolution is underway with a targeted fix expected within the sprint.","technicalSummary":"The root cause appears to be a race condition in the database connection pool under high concurrency. Connection timeout exceptions are propagating to the UI layer. Proposed fix involves increasing pool size and adding retry logic with exponential backoff.","simpleExplanation":"The system is experiencing slowdowns because too many users are trying to use it at the same time and the database cannot keep up. The team is working on a fix to handle more users simultaneously.","keyDecisions":["Increase connection pool from 50 to 200","Add retry logic with 3 attempts","Deploy hotfix to production during maintenance window"],"blockers":["Waiting for DBA approval on pool size change","Production deployment window not yet confirmed"],"risks":["Hotfix may require restart affecting active sessions","Pool size increase may impact memory on DB server"],"actionItems":["DBA to approve connection pool change by EOD","DevOps to schedule maintenance window","QA to run load tests against staging"],"completionConfidencePercent":72}""";

        if (lowerSystem.Contains("duplicate") || lowerSystem.Contains("similarity"))
            return """{"duplicates":[{"index":1,"similarityPercent":78,"reason":"Both tickets describe connection timeout issues in the same module"},{"index":3,"similarityPercent":62,"reason":"Similar symptoms reported with database under load"}]}""";

        if (lowerSystem.Contains("meeting") || lowerSystem.Contains("extract"))
            return """{"meetingTitle":"Sprint Planning Meeting","narrativeSummary":"The team reviewed current sprint progress and identified three critical action items. Resource allocation was discussed with the decision to reassign two tickets. Dependencies on the payment gateway integration were flagged as risks.","tasks":[{"title":"Fix payment gateway timeout","description":"Investigate and resolve the 30-second timeout occurring in production","owner":"Ahmad","dueDate":"2026-06-20","priority":"High"},{"title":"Update API documentation","description":"Document the new endpoints added in this sprint","owner":"Sara","dueDate":"2026-06-25","priority":"Medium"}],"decisions":[{"decision":"Move to microservices architecture for the notification module","context":"Current monolithic approach causing deployment bottlenecks","owner":"Tech Lead"}],"risks":["Third-party payment API has announced deprecation of v1 endpoints","Team capacity reduced next week due to planned leave"],"dependencies":["Payment gateway team must provide updated API credentials","DevOps must provision staging environment before testing"]}""";

        if (lowerSystem.Contains("ticket") && lowerSystem.Contains("draft"))
            return """{"suggestedTitle":"Payment Gateway Integration Timeout Error","suggestedDescription":"## Problem\nUsers are experiencing timeout errors when attempting to process payments through the payment gateway integration.\n\n## Steps to Reproduce\n1. Navigate to checkout\n2. Enter payment details\n3. Click Submit\n4. Observe timeout after 30 seconds\n\n## Expected Behavior\nPayment should process within 5 seconds\n\n## Actual Behavior\nRequest times out after 30 seconds with no feedback to user\n\n## Impact\nHigh - blocking payment processing for all customers","suggestedPriority":"High","suggestedIssueType":"Bug","suggestedLabels":["payment","integration","timeout","production"],"suggestedAssigneeName":null,"suggestedDueDate":null}""";

        if (lowerSystem.Contains("filter") || lowerSystem.Contains("search") || lowerSystem.Contains("translate"))
            return """{"interpretation":"Showing overdue high priority tickets assigned to team members in the Engineering department","assigneeName":null,"priorityName":"High","statusName":null,"issueTypeName":null,"departmentName":"Engineering","labelName":null,"overdueOnly":true,"unassignedOnly":false,"freeTextSearch":null}""";

        if (lowerSystem.Contains("comment") || lowerSystem.Contains("reply"))
            return "Thank you for the update. I have reviewed the issue and can confirm the root cause has been identified. The engineering team is currently implementing the fix and we expect to have it deployed to the staging environment within the next 2 hours for validation. I will provide another update once testing is complete.";

        if (lowerSystem.Contains("executive") || lowerSystem.Contains("report"))
            return """{"narrativeSummary":"This period saw a 12% increase in ticket volume with the majority concentrated in the infrastructure and integration categories. Resolution times improved by 8% compared to the previous period, indicating effective process improvements. SLA compliance remains strong at 94.2% despite the increased load.","teamPerformanceOverview":"The development team demonstrated strong throughput this period, resolving 67 tickets with an average resolution time of 18 hours. The DevOps team successfully handled all critical incidents within SLA windows. Some workload imbalance was noted with two team members carrying disproportionate ticket loads.","slaHealthReport":"SLA compliance at 94.2% is above the 90% target threshold. The 8 SLA breaches this period were concentrated in the integration category and were primarily caused by third-party vendor delays outside team control.","deliveryRisks":["Payment gateway migration still 2 sprints behind schedule","3 high-priority tickets without assignees risk further delays","Database upgrade dependency not yet confirmed by infrastructure team"],"resourceBottlenecks":["Ahmad handling 35% of all infrastructure tickets","QA team understaffed for current sprint volume","No dedicated security reviewer for compliance tickets"],"topRecurringCategories":["Database connectivity issues (14 tickets)","Authentication/SSO failures (9 tickets)","API integration timeouts (8 tickets)"]}""";

        if (lowerSystem.Contains("sprint") || lowerSystem.Contains("risk"))
            return """{"atRiskTickets":[{"ticketKey":"IT-1042","riskReason":"High priority with no assignee and due in 2 days","severity":"Critical"},{"ticketKey":"IT-1038","riskReason":"SLA will breach within 4 hours","severity":"High"}],"agingTickets":[{"ticketKey":"IT-1021","riskReason":"No update for 12 days, still In Progress","severity":"Medium"},{"ticketKey":"IT-1015","riskReason":"Reopened 3 times with no root cause documented","severity":"High"}],"workloadImbalance":[{"assigneeName":"Ahmad","assessment":"Overloaded with 9 active tickets — consider redistributing 3-4 tickets"},{"assigneeName":"Sara","assessment":"Under-utilized with only 1 active ticket — available for additional assignments"}],"slaBreachWarnings":["IT-1038: SLA breach in 3.5 hours","IT-1044: SLA breach in 6 hours if not updated"],"recommendedActions":["Immediately assign IT-1042 to an available team member","Escalate IT-1038 to team lead for priority handling","Schedule async review of IT-1021 with the original assignee","Redistribute 3 tickets from Ahmad to Sara to balance workload"]}""";

        if (lowerSystem.Contains("knowledge") || lowerSystem.Contains("answer"))
            return """{"answer":"Based on the ticket history, the payment gateway failures were primarily caused by a misconfigured SSL certificate that expired on March 15th. The issue was first reported in IT-1023 and was resolved by renewing the certificate and implementing automatic renewal alerts. A follow-up ticket (IT-1041) added monitoring to prevent recurrence.","citations":[{"index":1,"relevance":"Original incident report documenting the SSL certificate expiration"},{"index":2,"relevance":"Resolution ticket with the fix implementation details"}]}""";

        if (lowerSystem.Contains("similar") || lowerSystem.Contains("related"))
            return """{"related":[{"index":1,"similarityPercent":82,"reason":"Same component, similar error pattern"},{"index":4,"similarityPercent":71,"reason":"Related infrastructure issue in same time period"}],"similarIncidents":[{"index":2,"similarityPercent":91,"reason":"Identical root cause - database connection pool exhaustion"},{"index":7,"similarityPercent":65,"reason":"Similar symptoms observed under high load"}],"previousResolutions":["Increased connection pool size to 200 connections resolved similar issue in IT-892","Adding connection retry logic with exponential backoff prevented recurrence in IT-743","Database index optimization reduced query times by 60% in IT-651"]}""";

        return """{"result":"AI analysis complete. The requested analysis has been performed successfully.","confidence":85}""";
    }
}
