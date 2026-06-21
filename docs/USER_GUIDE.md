# ITS User Guide

Welcome to the **Internal Ticketing System (ITS)** — your team's central hub for tracking tasks, bugs, and projects.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Dashboard Overview](#dashboard-overview)
3. [Projects](#projects)
4. [Tickets](#tickets)
5. [Ticket Fields Reference](#ticket-fields-reference)
6. [Kanban Board](#kanban-board)
7. [Comments](#comments)
8. [Attachments](#attachments)
9. [Notifications](#notifications)
10. [AI Features](#ai-features)
11. [Tips and Best Practices](#tips-and-best-practices)

---

## Getting Started

### Logging In

ITS uses your corporate Active Directory credentials — the same username and password you use for your computer.

1. Open your browser and navigate to the ITS URL provided by your administrator.
2. Enter your **User Principal Name** (e.g. `john.smith@yourcompany.com`) and **password**.
3. Click **Sign In**.

Your session is valid for **8 hours**. After that, you will be asked to log in again. There is no "Remember Me" option — this is by design to comply with corporate security policy.

### Logging Out

Click your **avatar** in the top-right corner, then select **Sign Out**. Closing the browser tab does not log you out — always sign out when using a shared computer.

---

## Dashboard Overview

The Dashboard is your home screen. It shows:

| Section | Description |
|---------|-------------|
| **My Open Tickets** | Tickets assigned to you that are not yet Done or Cancelled |
| **Tickets I'm Watching** | Tickets you have subscribed to (regardless of assignee) |
| **Recent Activity** | The latest changes across your projects |
| **Overdue Tickets** | Tickets past their due date assigned to you |
| **Projects Summary** | A quick count of open tickets per project |

Click any ticket key (e.g. **ALPHA-42**) to open the full ticket detail view.

---

## Projects

### Viewing Projects

Click **Projects** in the left navigation to see all projects you are a member of. Each card shows:
- Project name and key
- Number of open tickets
- Project Lead name

### Creating a Project

Only **System Administrators** and **Project Leads** can create projects.

1. Click **Projects** → **New Project**.
2. Fill in the project name, key (2–6 uppercase letters, e.g. `ALPHA`), description, and assign a lead.
3. Click **Create**.

> **Note:** The project key is permanent and used as a prefix for all ticket numbers (e.g. `ALPHA-1`). Choose it carefully.

---

## Tickets

### Creating a Ticket

1. Open a project or click **Create Ticket** from any page.
2. Fill in the required fields: **Title**, **Issue Type**, and **Project**.
3. Optionally add: Description, Priority, Assignee, Due Date, Story Points, and Labels.
4. Click **Create**.

The ticket is assigned a sequential key (e.g. `ALPHA-7`) automatically.

### Viewing a Ticket

Click a ticket key anywhere in the application to open the full detail view, which shows:
- All ticket fields
- Current workflow status and available transitions
- Comment thread
- Attached files
- Watchers list
- Activity timeline (every change recorded)
- AI-generated summary (if AI is enabled)

### Editing a Ticket

Click the **pencil icon** or any editable field directly to make changes. Fields are saved individually (inline edit) for most properties. For a full edit form, click **Edit** in the ticket actions menu.

> **Optimistic concurrency:** ITS prevents conflicting edits. If someone else changes the ticket while you are editing it, you will see a conflict warning and can reload to get the latest version.

### Deleting a Ticket

Only **Project Leads** and **System Administrators** can delete tickets. Deletion is soft — the ticket is hidden from views but remains in the database for audit purposes.

### Transitioning a Ticket (Changing Status)

1. Open a ticket.
2. Click the **Status** button (shows the current status, e.g. "To Do").
3. Select the next status from the allowed transitions.
4. If required, add a comment or select a resolution, then confirm.

The available transitions depend on the project workflow configured by your administrator.

### Filtering and Searching Tickets

From the **Tickets** list view, use the filter bar to narrow results by:
- **Project**
- **Assignee**
- **Priority**
- **Issue Type**
- **Status**
- **Text search** (searches title and description)

Combine multiple filters for precise results. Use **AI Natural Language Search** for free-text queries like "overdue tickets assigned to Alice in the last month" (see [AI Features](#ai-features)).

---

## Ticket Fields Reference

| Field | Description | Required |
|-------|-------------|----------|
| **Title** | One-line summary of the issue | Yes |
| **Description** | Full details, steps to reproduce, etc. Supports Markdown | No |
| **Issue Type** | Bug, Story, Task, Epic, etc. (configured per project) | Yes |
| **Priority** | Critical, High, Medium, Low | No (defaults to Medium) |
| **Assignee** | The user responsible for resolving the ticket | No |
| **Due Date** | Target completion date | No |
| **Story Points** | Effort estimate (numeric) | No |
| **Estimated Hours** | Time estimate in hours | No |
| **Labels** | Tags for grouping and filtering (multi-select) | No |
| **Watchers** | Users who receive notifications about this ticket | No |
| **Status** | Current workflow state (e.g. Backlog, In Progress, Done) | Auto |
| **Ticket Key** | Auto-generated identifier (e.g. ALPHA-42) | Auto |

---

## Kanban Board

The Kanban board gives a visual overview of all tickets in a project, organised by status.

### Accessing the Board

Navigate to a project and click the **Board** tab.

### Columns

Each column represents a workflow status. The column header shows the status name and the current ticket count. If a **WIP limit** is configured (Work In Progress limit), the header turns red when the limit is exceeded.

### Drag and Drop

Drag a ticket card from one column to another to transition it to a new status. If the transition requires a comment or resolution, a dialog will appear before confirming the move.

> Only transitions allowed by the project workflow can be made via drag-and-drop.

### Board Filters

Use the filter bar above the board to show only:
- Tickets assigned to a specific person
- A specific priority
- A specific label
- A specific issue type
- Tickets matching a text search

---

## Comments

### Adding a Comment

Open a ticket and scroll to the **Comments** section. Type your comment in the box and click **Post Comment**.

Comments support **Markdown**:
- `**bold**` → **bold**
- `_italic_` → _italic_
- `` `code` `` → `code`
- `- item` → bullet list
- `1. item` → numbered list
- ` ```code block``` `

### Replying to a Comment

Click the **Reply** button on any comment to create a threaded reply.

### Editing and Deleting Comments

You can edit or delete your own comments by clicking the **…** menu on the comment. Administrators can delete any comment.

---

## Attachments

### Uploading Files

1. Open a ticket.
2. Click **Attach File** or drag and drop files onto the ticket detail area.
3. Supported formats: images (JPEG, PNG, GIF, WebP), PDF, Word, Excel, plain text, CSV, ZIP, MP4.
4. Maximum file size: **25 MB per file**.

### Downloading Attachments

Click an attachment thumbnail or filename to download it. Images are previewed inline.

### Deleting Attachments

Click the **delete icon** on an attachment. Only the uploader, Project Leads, and System Administrators can delete attachments.

---

## Notifications

ITS sends notifications when:
- A ticket is assigned to you
- A ticket you are watching changes status
- Someone comments on a ticket you are watching or were assigned to
- Your ticket's due date is approaching
- Someone mentions you in a comment

### Viewing Notifications

Click the **bell icon** in the top navigation bar. The number badge shows your unread notification count.

Click a notification to navigate directly to the relevant ticket.

### Marking as Read

- Click **Mark as Read** on an individual notification.
- Click **Mark All as Read** to clear all notifications at once.

---

## AI Features

ITS integrates with AI to help you work faster. These features require AI to be enabled by your administrator.

> AI responses are suggestions only — always review before using them.

### AI Ticket Summary

When viewing a ticket, click **AI Summary** to generate a structured summary including:
- Executive summary (one paragraph)
- Open blockers
- Action items
- Suggested next steps

The summary is cached and can be refreshed by clicking **Refresh Summary**.

### Similar Tickets

When viewing a ticket, the **Similar Tickets** panel shows semantically related tickets. Use this to:
- Find existing tickets that might be duplicates before creating a new one.
- Discover related context from past work.

### Duplicate Detection

When creating a new ticket, click **Check for Duplicates** to find existing tickets with similar titles and descriptions. Review the suggestions before submitting — you may want to add a comment to an existing ticket instead.

### AI Comment Generator

Inside a ticket's comment box, click **AI Draft** to ask the AI to write a comment for you. Provide an instruction and tone preference (Professional, Casual, Technical), and the AI will generate a draft. **The draft is never posted automatically** — you must review and click **Post Comment** yourself.

### Natural Language Search

In the search bar, toggle **AI Search** and type a plain English query:
- "All critical bugs assigned to Bob that are overdue"
- "Tickets in the Alpha project with no assignee created this month"
- "High priority stories not yet in review"

The AI translates your query into structured filters and runs the search.

### Ticket Drafter

Click **New Ticket** → **Draft from Text** to paste an email, meeting note, or free-text description. The AI will propose a ticket title, description, issue type, and priority. Review and edit before saving.

### Meeting Note Parser

Click **Parse Meeting Notes** from the project view or the AI menu. Paste your meeting notes, and the AI will extract:
- Action items (with suggested ticket drafts)
- Decisions made
- Risks identified
- Dependencies noted

You can then create tickets directly from the extracted action items.

### AI Executive Report

Available to **Project Leads** and **System Administrators**. Navigate to **Reports** → **AI Executive Report**. Select a time period (weekly, monthly, quarterly) and optionally filter by project or department. The report includes:
- Ticket volume trends
- SLA compliance summary
- Top contributors
- Recurring problem themes

### Sprint Intelligence

Available per project. Navigate to a project → **Sprint Intelligence** to see:
- Tickets at risk of missing SLA
- Workload imbalance across team members
- Recommendations for reassignment

---

## Tips and Best Practices

**Write descriptive ticket titles.** A good title tells anyone — at a glance — exactly what the problem or task is. Compare:
- Poor: "Fix bug"
- Good: "Login page throws 500 error when password contains special characters"

**Use Markdown in descriptions.** Structure your descriptions with headings, bullet points, and code blocks. This makes tickets easier to scan and more useful as a reference.

**Set a due date when there is a real deadline.** Due dates trigger SLA tracking and overdue alerts. Only set them when they reflect an actual commitment.

**Use Labels to group related work.** Labels like `security`, `performance`, `customer-reported`, or `tech-debt` make it easy to filter and report on themes.

**Add yourself as a watcher.** If you are involved in a ticket but are not the assignee, click **Watch** to receive notifications about updates.

**Use AI Duplicate Detection before creating new tickets.** Duplicate tickets waste time and split the conversation. Always check for similar tickets first.

**Transition tickets as you work.** Keeping ticket statuses accurate gives your team real-time visibility on what is in progress, what is blocked, and what is done. Move tickets to "In Progress" when you start working, not when you finish.

**Add a comment when transitioning to Done or Cancelled.** Briefly explain what was done or why the ticket was cancelled. This creates a useful audit trail for future reference.
