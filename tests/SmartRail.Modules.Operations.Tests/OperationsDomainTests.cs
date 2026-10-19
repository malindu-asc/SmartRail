using SmartRail.Modules.Operations.Domain.Disruptions;
using SmartRail.Modules.Operations.Domain.Incidents;
using SmartRail.Modules.Operations.Domain.Messages;

namespace SmartRail.Modules.Operations.Tests;

public sealed class DisruptionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private static Disruption Notice(bool networkWide, bool submit = true) =>
        Disruption.Create("Signal fault", "Delays of 20 minutes", DisruptionSeverity.Major, networkWide, networkWide ? [] : ["MIL"],
            null, Now.AddHours(2), "author", Now, submit: submit).Value;

    [Fact]
    public void A_local_notice_publishes_at_once()
    {
        Assert.Equal(DisruptionStatus.Published, Notice(false).Status);
    }

    [Fact]
    public void A_network_wide_notice_waits_for_a_second_person()
    {
        var notice = Notice(true);
        Assert.Equal(DisruptionStatus.AwaitingApproval, notice.Status);
        Assert.True(notice.Approve("author", Now).IsFailure);
        Assert.True(notice.Approve("someone-else", Now).IsSuccess);
        Assert.Equal(DisruptionStatus.Published, notice.Status);
    }

    [Fact]
    public void Rejecting_needs_a_comment_and_lets_the_author_send_it_again()
    {
        var notice = Notice(true);
        Assert.True(notice.Reject("other", " ", Now).IsFailure);
        Assert.True(notice.Reject("other", "Wording unclear", Now).IsSuccess);
        Assert.Equal(DisruptionStatus.Rejected, notice.Status);
        Assert.True(notice.Submit(Now).IsSuccess);
        Assert.Equal(DisruptionStatus.AwaitingApproval, notice.Status);
    }

    [Fact]
    public void A_draft_does_not_publish_until_submitted()
    {
        var draft = Notice(false, submit: false);
        Assert.Equal(DisruptionStatus.Draft, draft.Status);
        Assert.True(draft.Submit(Now).IsSuccess);
        Assert.Equal(DisruptionStatus.Published, draft.Status);
    }

    [Fact]
    public void A_live_network_wide_notice_cannot_be_edited_but_a_local_one_can()
    {
        var wide = Notice(true);
        wide.Approve("other", Now);
        Assert.True(wide.Update("New", "Text", DisruptionSeverity.Minor, null, Now.AddHours(1), Now).IsFailure);

        var local = Notice(false);
        Assert.True(local.Update("New", "Text", DisruptionSeverity.Minor, null, Now.AddHours(1), Now).IsSuccess);
    }

    [Fact]
    public void A_closed_notice_stays_closed()
    {
        var notice = Notice(false);
        Assert.True(notice.Retract(Now).IsSuccess);
        Assert.True(notice.Resolve(Now).IsFailure);
    }

    [Fact]
    public void A_notice_needs_a_scope_a_future_review_time_and_plain_text()
    {
        Assert.True(Disruption.Create("T", "M", DisruptionSeverity.Minor, false, [], null, Now.AddHours(1), "a", Now).IsFailure);
        Assert.True(Disruption.Create("T", "M", DisruptionSeverity.Minor, true, [], null, Now.AddHours(-1), "a", Now).IsFailure);
        Assert.True(Disruption.Create("", "M", DisruptionSeverity.Minor, true, [], null, Now.AddHours(1), "a", Now).IsFailure);
    }

    [Fact]
    public void It_is_expired_once_the_review_time_has_passed()
    {
        var notice = Notice(false);
        Assert.False(notice.IsExpired(Now.AddHours(1)));
        Assert.True(notice.IsExpired(Now.AddHours(3)));
    }
}

public sealed class IncidentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private static Incident Open() => Incident.Declare("Signal failure", IncidentSeverity.Major, "alice", null, ["MIL"], null, Now).Value;

    [Fact]
    public void The_person_who_declares_it_owns_it_and_the_timeline_starts()
    {
        var incident = Open();
        Assert.Equal("alice", incident.OwnerSubject);
        Assert.Single(incident.Entries);
    }

    [Fact]
    public void Handing_over_needs_a_note_and_a_different_person()
    {
        var incident = Open();
        Assert.True(incident.HandOver("bob", "", "alice", Now).IsFailure);
        Assert.True(incident.HandOver("alice", "Back to me", "alice", Now).IsFailure);
        Assert.True(incident.HandOver("bob", "Shift change", "alice", Now).IsSuccess);
        Assert.Equal("bob", incident.OwnerSubject);
    }

    [Fact]
    public void A_resolved_incident_takes_no_more_changes()
    {
        var incident = Open();
        Assert.True(incident.Resolve("Fixed", "alice", Now).IsSuccess);
        Assert.True(incident.AddNote(IncidentEntryKind.Note, "late note", null, "alice", Now).IsFailure);
        Assert.True(incident.Resolve("again", "alice", Now).IsFailure);
    }

    [Fact]
    public void Resolving_needs_a_note()
    {
        Assert.True(Open().Resolve(" ", "alice", Now).IsFailure);
    }
}

public sealed class ControlMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private static ControlMessage Send(bool ack = true) =>
        ControlMessage.Send("driver-1", null, "1004", "DEMO-1", null, null, "Hold at Mill Lane", ack, Now.AddMinutes(30), "ctrl", Now).Value;

    [Fact]
    public void It_moves_from_sent_to_delivered_to_acknowledged()
    {
        var message = Send();
        Assert.Equal(MessageState.Sent, message.StateAt(Now));
        message.MarkDelivered(Now.AddMinutes(1));
        Assert.Equal(MessageState.Delivered, message.StateAt(Now));
        Assert.True(message.Acknowledge(Now.AddMinutes(2)).IsSuccess);
        Assert.Equal(MessageState.Acknowledged, message.StateAt(Now));
    }

    [Fact]
    public void An_expired_message_cannot_be_acknowledged()
    {
        Assert.True(Send().Acknowledge(Now.AddHours(1)).IsFailure);
    }

    [Fact]
    public void It_is_overdue_only_when_an_acknowledgement_is_needed_and_late()
    {
        var needsAck = Send();
        Assert.False(needsAck.IsOverdue(Now.AddMinutes(1), TimeSpan.FromMinutes(5)));
        Assert.True(needsAck.IsOverdue(Now.AddMinutes(6), TimeSpan.FromMinutes(5)));
        Assert.False(Send(ack: false).IsOverdue(Now.AddMinutes(6), TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void A_message_needs_a_driver_a_short_text_and_a_future_end()
    {
        Assert.True(ControlMessage.Send("", null, null, null, null, null, "x", true, Now.AddMinutes(5), "c", Now).IsFailure);
        Assert.True(ControlMessage.Send("d", null, null, null, null, null, "", true, Now.AddMinutes(5), "c", Now).IsFailure);
        Assert.True(ControlMessage.Send("d", null, null, null, null, null, "x", true, Now.AddMinutes(-5), "c", Now).IsFailure);
    }

    [Fact]
    public void Resending_counts_and_restarts_delivery()
    {
        var message = Send();
        message.MarkDelivered(Now.AddMinutes(1));
        Assert.True(message.Resend(Now.AddMinutes(10), Now.AddMinutes(40)).IsSuccess);
        Assert.Equal(1, message.ResentCount);
        Assert.Equal(MessageState.Sent, message.StateAt(Now.AddMinutes(10)));
    }
}

public sealed class AssistanceRequestTests
{
    [Fact]
    public void A_request_is_handled_once()
    {
        var request = AssistanceRequest.Create("driver-1", null, "1004", "mil", "Unwell passenger", DateTimeOffset.UtcNow);
        Assert.Equal("MIL", request.StationCode);
        Assert.True(request.Handle("ctrl", "Ambulance called", DateTimeOffset.UtcNow).IsSuccess);
        Assert.True(request.Handle("ctrl", null, DateTimeOffset.UtcNow).IsFailure);
    }
}
