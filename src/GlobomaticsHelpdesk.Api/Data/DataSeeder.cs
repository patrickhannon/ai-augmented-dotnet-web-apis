using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Domain;

namespace GlobomaticsHelpdesk.Api.Data;

public static class DataSeeder
{
    public static void Seed(HelpdeskDbContext db)
    {
        if (db.Users.Any() || db.Agents.Any() || db.Tickets.Any() || db.KnowledgeArticles.Any())
        {
            return;
        }

        var seedTime = new DateTimeOffset(2026, 4, 15, 9, 0, 0, TimeSpan.Zero);

        // Users
        var users = new List<User>
        {
            new() { Id = SeededIdentities.AdminUserId,    DisplayName = "Avery Admin",   Email = "avery.admin@globomatics.example",   Department = "IT" },
            new() { Id = SeededIdentities.ReadOnlyUserId, DisplayName = "Quinn Viewer",  Email = "quinn.viewer@globomatics.example",  Department = "Operations" },
            new() { Id = U("u-jordan"),                   DisplayName = "Jordan Patel",  Email = "jordan.patel@globomatics.example",  Department = "Sales" },
            new() { Id = U("u-morgan"),                   DisplayName = "Morgan Lee",    Email = "morgan.lee@globomatics.example",    Department = "Marketing" },
            new() { Id = U("u-casey"),                    DisplayName = "Casey Nguyen",  Email = "casey.nguyen@globomatics.example",  Department = "Engineering" },
            new() { Id = U("u-taylor"),                   DisplayName = "Taylor Brooks", Email = "taylor.brooks@globomatics.example", Department = "Finance" },
            new() { Id = U("u-sam"),                      DisplayName = "Sam Rivera",    Email = "sam.rivera@globomatics.example",    Department = "HR" },
            new() { Id = U("u-priya"),                    DisplayName = "Priya Shah",    Email = "priya.shah@globomatics.example",    Department = "Engineering" },
            new() { Id = U("u-devon"),                    DisplayName = "Devon Carter",  Email = "devon.carter@globomatics.example",  Department = "Support" }
        };

        // Agents
        var agents = new List<Agent>
        {
            new() { Id = SeededIdentities.PrimaryAgentId, DisplayName = "Riley Reynolds", Email = "riley.reynolds@globomatics.example", Specialty = "Identity & Access" },
            new() { Id = U("a-jamie"),                    DisplayName = "Jamie Okafor",   Email = "jamie.okafor@globomatics.example",   Specialty = "Networking" },
            new() { Id = U("a-alex"),                     DisplayName = "Alex Romero",    Email = "alex.romero@globomatics.example",    Specialty = "Endpoints" },
            new() { Id = U("a-sasha"),                    DisplayName = "Sasha Park",     Email = "sasha.park@globomatics.example",     Specialty = "Collaboration & M365" }
        };

        db.Users.AddRange(users);
        db.Agents.AddRange(agents);

        // Knowledge articles
        var kb = new List<KnowledgeArticle>
        {
            Article("k-vpn",        "Connecting to the Globomatics VPN",
                "To connect, install GloboConnect, sign in with SSO, then approve the MFA prompt. If you cannot connect, restart GloboConnect and confirm you are not on a corporate Wi-Fi (which already routes internally).",
                new[] { "vpn", "remote-work", "network" }, seedTime.AddDays(-30)),

            Article("k-pwreset",    "Resetting your password",
                "Use https://passwords.globomatics.example to reset. You'll need access to your registered MFA device. Passwords must be at least 14 characters and cannot match the previous 5 passwords.",
                new[] { "password", "identity", "self-service" }, seedTime.AddDays(-25)),

            Article("k-mfa",        "Registering a new MFA device",
                "Open the GloboID portal, go to Security > MFA Devices, choose Add Device, then scan the QR code with Microsoft Authenticator or your hardware key.",
                new[] { "mfa", "identity", "security" }, seedTime.AddDays(-20)),

            Article("k-printer",    "Adding the office printer",
                "Open Settings > Printers > Add a printer. Choose 'GLOBO-PRINT-01' from the network list. If it doesn't appear, ensure you're on the corporate network or VPN, then run 'globofix printers' from a terminal.",
                new[] { "printer", "hardware", "office" }, seedTime.AddDays(-18)),

            Article("k-laptop",     "Requesting a laptop refresh",
                "Laptops are refreshed every 3 years. To request a new device, open a ticket with category 'Hardware > Laptop refresh' and include your current asset tag.",
                new[] { "laptop", "hardware", "asset" }, seedTime.AddDays(-14)),

            Article("k-onboarding", "New hire IT onboarding checklist",
                "Day 1: pick up laptop, sign in to GloboID, register MFA, install GloboConnect VPN, join #welcome on Teams. Day 2: complete security training. Day 3: meet your IT buddy.",
                new[] { "onboarding", "hr", "checklist" }, seedTime.AddDays(-12)),

            Article("k-email",      "Email is not syncing on mobile",
                "Open Outlook mobile, go to Settings > Account > Reset account. If the issue persists, remove and re-add the account. Mobile devices must be enrolled in Intune to receive corporate email.",
                new[] { "email", "outlook", "mobile" }, seedTime.AddDays(-10)),

            Article("k-software",   "Installing approved software",
                "Use the Globomatics Self-Service portal in Company Portal. If the software you need isn't listed, file a ticket under 'Software > New request' with a business justification.",
                new[] { "software", "self-service", "intune" }, seedTime.AddDays(-9)),

            Article("k-meetingroom","Booking a meeting room",
                "Open Outlook > New Meeting > Room Finder. Choose your office, building, and capacity. Rooms named 'GLOBO-CONF-*' include video conferencing.",
                new[] { "meeting", "rooms", "outlook" }, seedTime.AddDays(-7)),

            Article("k-phishing",   "Reporting a suspected phishing email",
                "Use the 'Report Phishing' button in Outlook. The Security team is notified automatically. Do not reply to or click links in the message. Forward attachments only if asked.",
                new[] { "security", "phishing", "email" }, seedTime.AddDays(-3))
        };
        db.KnowledgeArticles.AddRange(kb);

        // Tickets — mix of statuses, priorities, assignees, with comments
        var tickets = new List<Ticket>();

        Ticket Make(string idKey, Guid requester, Guid? assignee, string title, string description,
            TicketStatus status, TicketPriority priority, int daysAgoCreated, int daysAgoUpdated)
        {
            var t = new Ticket
            {
                Id = U(idKey),
                Title = title,
                Description = description,
                Status = status,
                Priority = priority,
                RequesterId = requester,
                AssignedAgentId = assignee,
                CreatedAt = seedTime.AddDays(-daysAgoCreated),
                UpdatedAt = seedTime.AddDays(-daysAgoUpdated)
            };
            tickets.Add(t);
            return t;
        }

        var t1 = Make("t-001", U("u-jordan"), SeededIdentities.PrimaryAgentId,
            "Cannot connect to VPN from home",
            "GloboConnect keeps timing out after entering MFA. I'm on home Wi-Fi, restarted twice.",
            TicketStatus.Open, TicketPriority.High, 6, 1);
        t1.Comments.Add(C(t1.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Asked Jordan to try resetting their MFA token and reinstall GloboConnect.", false, seedTime.AddDays(-5)));
        t1.Comments.Add(C(t1.Id, body: "Reinstalled — still failing. Error code 0x80072EFD.",
            isInternal: false, createdAt: seedTime.AddDays(-3), userId: U("u-jordan")));
        t1.Comments.Add(C(t1.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Internal: Looks like a cert pinning issue, escalating to network team.", true, seedTime.AddDays(-1)));

        var t2 = Make("t-002", U("u-morgan"), U("a-alex"),
            "Need a docking station for new monitor",
            "I picked up a second monitor but don't have enough USB-C ports. Can I get a Thunderbolt dock?",
            TicketStatus.Pending, TicketPriority.Normal, 9, 2);
        t2.Comments.Add(C(t2.Id, agentId: U("a-alex"),
            "Submitted a hardware order. ETA 5 business days.", false, seedTime.AddDays(-2)));

        var t3 = Make("t-003", U("u-casey"), null,
            "Email not syncing on phone",
            "Outlook on iOS stopped syncing this morning. I can still send/receive on the laptop.",
            TicketStatus.New, TicketPriority.Normal, 1, 1);

        var t4 = Make("t-004", U("u-taylor"), U("a-sasha"),
            "Add me to the FY26-Planning Teams group",
            "Finance director said I should be in the planning channel.",
            TicketStatus.Resolved, TicketPriority.Low, 14, 8);
        t4.Comments.Add(C(t4.Id, agentId: U("a-sasha"),
            "Added Taylor to FY26-Planning. Confirmed access.", false, seedTime.AddDays(-9)));
        t4.Comments.Add(C(t4.Id, body: "Confirmed, thanks!",
            isInternal: false, createdAt: seedTime.AddDays(-8), userId: U("u-taylor")));

        var t5 = Make("t-005", U("u-sam"), SeededIdentities.PrimaryAgentId,
            "MFA device replacement",
            "I lost my phone and need to register a new MFA device. I've already filed an HR incident.",
            TicketStatus.Open, TicketPriority.Urgent, 2, 0);
        t5.Comments.Add(C(t5.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Verified identity over recorded video call. Issued temporary access pass.", true, seedTime.AddDays(-1)));

        var t6 = Make("t-006", U("u-priya"), U("a-jamie"),
            "Office Wi-Fi keeps dropping in conference room GLOBO-CONF-3",
            "Calls drop every ~10 minutes when meeting in CONF-3. Other rooms are fine.",
            TicketStatus.Open, TicketPriority.High, 4, 1);
        t6.Comments.Add(C(t6.Id, agentId: U("a-jamie"),
            "Tagged AP near CONF-3 for replacement. Will swap during after-hours window.", false, seedTime.AddDays(-1)));

        var t7 = Make("t-007", U("u-devon"), U("a-alex"),
            "Laptop fan very loud",
            "Fan runs at full speed even with no apps open. Laptop is 3 years old.",
            TicketStatus.Pending, TicketPriority.Normal, 11, 5);
        t7.Comments.Add(C(t7.Id, agentId: U("a-alex"),
            "Eligible for laptop refresh. Coordinating swap.", false, seedTime.AddDays(-5)));

        var t8 = Make("t-008", U("u-jordan"), null,
            "Request access to Salesforce production",
            "Need read-only access for the new account dashboard.",
            TicketStatus.New, TicketPriority.Low, 1, 1);

        var t9 = Make("t-009", U("u-morgan"), SeededIdentities.PrimaryAgentId,
            "SSO loop signing in to marketing analytics",
            "Browser keeps redirecting between SSO and the app. Tried Edge and Chrome.",
            TicketStatus.Open, TicketPriority.High, 3, 0);
        t9.Comments.Add(C(t9.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Reproduced the loop. Will check the SAML config.", false, seedTime));

        var t10 = Make("t-010", U("u-casey"), U("a-alex"),
            "Install Visual Studio 2026",
            "Need it for the new build pipeline work.",
            TicketStatus.Resolved, TicketPriority.Normal, 20, 15);
        t10.Comments.Add(C(t10.Id, agentId: U("a-alex"),
            "Pushed install via Intune. Verified launches OK.", false, seedTime.AddDays(-15)));

        var t11 = Make("t-011", U("u-taylor"), U("a-sasha"),
            "OneDrive sync errors after laptop refresh",
            "After getting my new laptop, OneDrive shows 12 sync errors.",
            TicketStatus.Open, TicketPriority.Normal, 2, 0);

        var t12 = Make("t-012", U("u-sam"), null,
            "Phishing email reported",
            "Suspicious email pretending to be HR. Forwarded via Report Phishing button.",
            TicketStatus.Closed, TicketPriority.Low, 25, 22);
        t12.Comments.Add(C(t12.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Thanks — confirmed phishing. Blocked sender domain.", false, seedTime.AddDays(-23)));
        t12.Comments.Add(C(t12.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Closing ticket. No further action needed.", false, seedTime.AddDays(-22)));

        var t13 = Make("t-013", U("u-priya"), U("a-jamie"),
            "Static IP for the lab printer",
            "Lab printer keeps getting a new IP and breaking shared print queue.",
            TicketStatus.Pending, TicketPriority.Low, 16, 9);

        var t14 = Make("t-014", U("u-devon"), SeededIdentities.PrimaryAgentId,
            "Screen sharing not working in Teams",
            "When I try to share, I see a black screen. Reinstalled Teams.",
            TicketStatus.Open, TicketPriority.Normal, 5, 1);
        t14.Comments.Add(C(t14.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Asked Devon to grant Screen Recording permission in macOS Privacy settings.", false, seedTime.AddDays(-4)));

        var t15 = Make("t-015", U("u-jordan"), U("a-sasha"),
            "Distribution list update",
            "Please add me to sales-emea@globomatics.example.",
            TicketStatus.Resolved, TicketPriority.Low, 19, 18);

        var t16 = Make("t-016", U("u-morgan"), U("a-alex"),
            "Replacement keyboard",
            "A few keys stopped working on my keyboard.",
            TicketStatus.Closed, TicketPriority.Low, 30, 27);
        t16.Comments.Add(C(t16.Id, agentId: U("a-alex"),
            "Sent replacement via internal mail.", false, seedTime.AddDays(-28)));

        var t17 = Make("t-017", U("u-casey"), null,
            "Need Docker license bumped to team plan",
            "Hitting concurrent build limits.",
            TicketStatus.New, TicketPriority.Normal, 0, 0);

        var t18 = Make("t-018", U("u-taylor"), U("a-jamie"),
            "Conference room mic crackling",
            "Mic in CONF-2 has been crackling for two days.",
            TicketStatus.Open, TicketPriority.Normal, 3, 1);

        var t19 = Make("t-019", U("u-sam"), U("a-sasha"),
            "Booking system permissions",
            "Need 'edit' access on the booking calendar.",
            TicketStatus.Resolved, TicketPriority.Low, 8, 6);
        t19.Comments.Add(C(t19.Id, agentId: U("a-sasha"),
            "Granted edit permissions and confirmed.", false, seedTime.AddDays(-6)));

        var t20 = Make("t-020", U("u-priya"), SeededIdentities.PrimaryAgentId,
            "API key rotation",
            "Need to rotate the Globomatics-internal API keys for the platform team service principal.",
            TicketStatus.Open, TicketPriority.High, 2, 0);
        t20.Comments.Add(C(t20.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Internal: Rotation scheduled for tonight's change window.", true, seedTime));

        var t21 = Make("t-021", U("u-devon"), U("a-alex"),
            "External monitor flickers",
            "External monitor flickers when laptop is plugged in via the dock.",
            TicketStatus.Pending, TicketPriority.Normal, 7, 3);

        var t22 = Make("t-022", U("u-jordan"), null,
            "Shared mailbox access for sales-ops@",
            "Need read/write access for our team's shared mailbox.",
            TicketStatus.New, TicketPriority.Normal, 1, 1);

        var t23 = Make("t-023", U("u-morgan"), U("a-jamie"),
            "Slow Wi-Fi on the 4th floor",
            "Wi-Fi has been very slow on the 4th floor all week.",
            TicketStatus.Open, TicketPriority.High, 5, 2);

        var t24 = Make("t-024", U("u-casey"), U("a-sasha"),
            "Add new project board to Teams",
            "Need a new Planner board for project Helix.",
            TicketStatus.Resolved, TicketPriority.Low, 10, 9);

        var t25 = Make("t-025", U("u-taylor"), SeededIdentities.PrimaryAgentId,
            "MFA bypass for offline laptop",
            "Going on a wilderness trip — need a temporary offline access plan for laptop.",
            TicketStatus.Open, TicketPriority.Normal, 2, 1);
        t25.Comments.Add(C(t25.Id, agentId: SeededIdentities.PrimaryAgentId,
            "Issued time-limited offline access policy. Expires in 14 days.", true, seedTime.AddDays(-1)));

        // Suppress unused-variable warnings for tickets we don't add comments to.
        _ = (t3, t8, t11, t13, t15, t17, t18, t21, t22, t23, t24);

        db.Tickets.AddRange(tickets);
        db.SaveChanges();
    }

    private static Comment C(Guid ticketId, string body, bool isInternal, DateTimeOffset createdAt,
        Guid? userId = null, Guid? agentId = null) => new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorUserId = userId,
            AuthorAgentId = agentId,
            Body = body,
            IsInternalNote = isInternal,
            CreatedAt = createdAt
        };

    // Overload with named arguments for agent comments (most common case in seed data).
    private static Comment C(Guid ticketId, Guid agentId, string body, bool isInternal, DateTimeOffset createdAt)
        => C(ticketId, body, isInternal, createdAt, agentId: agentId);

    private static KnowledgeArticle Article(string idKey, string title, string body, IEnumerable<string> tags, DateTimeOffset updated)
        => new()
        {
            Id = U(idKey),
            Title = title,
            Body = body,
            Tags = tags.ToList(),
            UpdatedAt = updated
        };

    // Deterministic Guid from a short key — uses a stable namespace so the data
    // is identical across runs. Implementation: SHA1-based v5-style derivation
    // over a fixed namespace + key.
    private static readonly byte[] _ns =
        Guid.Parse("9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d").ToByteArray();
    private static Guid U(string key)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        Span<byte> input = stackalloc byte[_ns.Length + keyBytes.Length];
        _ns.CopyTo(input);
        keyBytes.CopyTo(input.Slice(_ns.Length));

        Span<byte> hash = stackalloc byte[20];
        System.Security.Cryptography.SHA1.HashData(input, hash);

        Span<byte> guidBytes = stackalloc byte[16];
        hash.Slice(0, 16).CopyTo(guidBytes);
        // Set version (5) and variant bits per RFC 4122 to keep the Guid well-formed.
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
