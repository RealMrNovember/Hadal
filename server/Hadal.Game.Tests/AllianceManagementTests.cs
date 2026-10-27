using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Ranks, join modes, invitations, applications and private messages (docs/rehber 10).</summary>
    public sealed class AllianceManagementTests
    {
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public AllianceManagementTests()
        {
            _world = TestSupport.World(_clock);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass })
                session.StateForTests.Wallet[type] = 50000;
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Where(d => d.Ack() != null).Last().Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete).Payload);

        private async Task<(PlayerSession Leader, RecordingConnection Connection, string AllianceId)> Found(string identity, string tag)
        {
            var (leader, connection) = await Connect(identity);
            Send(leader, new CreateAllianceCommand { CommandId = Id(), Name = "House " + tag, Tag = tag }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            return (leader, connection, Latest<AllianceDto>(connection, EntityIds.MyAlliance).Id);
        }

        private static AllianceMemberDto Member(RecordingConnection connection, PlayerSession session) =>
            Latest<AllianceDto>(connection, EntityIds.MyAlliance).Members.Single(m => m.PlayerId == session.PublicId);

        [Fact]
        public async Task The_founder_leads_and_newcomers_start_as_recruits()
        {
            var (leader, leaderConnection, allianceId) = await Found("rank-a", "RNKA");
            var (diver, _) = await Connect("rank-b");
            Send(diver, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);

            Assert.Equal(AlliancePermissions.Leader, Member(leaderConnection, leader).Rank);
            Assert.Equal(AlliancePermissions.Recruit, Member(leaderConnection, diver).Rank);
        }

        [Fact]
        public async Task Only_higher_ranks_promote_and_officer_seats_are_limited()
        {
            var (leader, leaderConnection, allianceId) = await Found("rank-c", "RNKC");
            var members = new List<(PlayerSession Session, RecordingConnection Connection)>();
            for (var i = 0; i < 6; i++)
            {
                var m = await Connect("rank-c" + i);
                Send(m.Session, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
                members.Add(m);
            }

            // A recruit cannot promote anyone.
            Send(members[0].Session, new SetMemberRankCommand { CommandId = Id(), MemberId = members[1].Session.PublicId, Rank = 3 }, CommandType.SetMemberRank);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(members[0].Connection).Reason);

            // The leader names four officers, with duties; a fifth seat does not exist.
            for (var i = 0; i < AlliancePermissions.MaxOfficers; i++)
            {
                Send(leader, new SetMemberRankCommand { CommandId = Id(), MemberId = members[i].Session.PublicId, Rank = 4, Role = "war" }, CommandType.SetMemberRank);
                Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            }

            Send(leader, new SetMemberRankCommand { CommandId = Id(), MemberId = members[4].Session.PublicId, Rank = 4 }, CommandType.SetMemberRank);
            Assert.Equal(nameof(SocialRejectReason.RankFull), LastAck(leaderConnection).Reason);
            Assert.Equal(("war", 4), (Member(leaderConnection, members[0].Session).Role, Member(leaderConnection, members[0].Session).Rank));

            // An officer may raise a recruit to veteran but not make officers, nor kick another officer.
            var officer = members[0];
            Send(officer.Session, new SetMemberRankCommand { CommandId = Id(), MemberId = members[4].Session.PublicId, Rank = 3 }, CommandType.SetMemberRank);
            Assert.Equal(CommandResultCode.Accepted, LastAck(officer.Connection).Result);
            Send(officer.Session, new SetMemberRankCommand { CommandId = Id(), MemberId = members[5].Session.PublicId, Rank = 4 }, CommandType.SetMemberRank);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(officer.Connection).Reason);
            Send(officer.Session, new KickMemberCommand { CommandId = Id(), MemberId = members[1].Session.PublicId }, CommandType.KickMember);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(officer.Connection).Reason);
            Send(officer.Session, new KickMemberCommand { CommandId = Id(), MemberId = members[5].Session.PublicId }, CommandType.KickMember);
            Assert.Equal(CommandResultCode.Accepted, LastAck(officer.Connection).Result);
        }

        [Fact]
        public async Task Leadership_can_be_handed_over()
        {
            var (leader, leaderConnection, allianceId) = await Found("lead-a", "LEDA");
            var (heir, heirConnection) = await Connect("lead-b");
            Send(heir, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);

            Send(leader, new TransferLeadershipCommand { CommandId = Id(), MemberId = heir.PublicId }, CommandType.TransferLeadership);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            var alliance = Latest<AllianceDto>(heirConnection, EntityIds.MyAlliance);
            Assert.Equal(heir.PublicId, alliance.LeaderId);
            Assert.Equal(AlliancePermissions.Leader, Member(heirConnection, heir).Rank);
            Assert.Equal(AlliancePermissions.Officer, Member(heirConnection, leader).Rank);
        }

        [Fact]
        public async Task Closed_alliances_take_invited_players_and_approval_ones_take_applications()
        {
            var (leader, leaderConnection, allianceId) = await Found("join-a", "JOIN");
            Send(leader, new SetAllianceSettingsCommand { CommandId = Id(), JoinMode = (int)AllianceJoinMode.Closed, Description = "quiet" }, CommandType.SetAllianceSettings);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);

            var (guest, guestConnection) = await Connect("join-b");
            Send(guest, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(nameof(SocialRejectReason.InviteOnly), LastAck(guestConnection).Reason);

            Send(leader, new InviteToAllianceCommand { CommandId = Id(), PlayerId = guest.PublicId }, CommandType.InviteToAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            var invite = Assert.Single(Latest<AllianceInviteListDto>(guestConnection, EntityIds.AllianceInvites).Invites);
            Assert.Equal(("JOIN", "join-a"), (invite.Tag, invite.InviterName));

            Send(guest, new AnswerInviteCommand { CommandId = Id(), AllianceId = allianceId, Accept = true }, CommandType.AnswerInvite);
            Assert.Equal(CommandResultCode.Accepted, LastAck(guestConnection).Result);
            Assert.Equal(allianceId, Latest<AllianceDto>(guestConnection, EntityIds.MyAlliance).Id);

            // Approval: the application waits for an R3+.
            Send(leader, new SetAllianceSettingsCommand { CommandId = Id(), JoinMode = (int)AllianceJoinMode.Approval }, CommandType.SetAllianceSettings);
            var (applicant, applicantConnection) = await Connect("join-c");
            Send(applicant, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(applicantConnection).Result);
            Assert.Contains(allianceId, Latest<AllianceInviteListDto>(applicantConnection, EntityIds.AllianceInvites).Applied);
            Assert.Single(Latest<AllianceDto>(leaderConnection, EntityIds.MyAlliance).Applicants);

            // A recruit cannot approve; the leader can.
            Send(guest, new AnswerApplicationCommand { CommandId = Id(), PlayerId = applicant.PublicId, Accept = true }, CommandType.AnswerApplication);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(guestConnection).Reason);
            Send(leader, new AnswerApplicationCommand { CommandId = Id(), PlayerId = applicant.PublicId, Accept = true }, CommandType.AnswerApplication);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            Assert.Equal(allianceId, Latest<AllianceDto>(applicantConnection, EntityIds.MyAlliance).Id);
            Assert.Equal(3, Latest<AllianceDto>(leaderConnection, EntityIds.MyAlliance).Members.Length);
        }

        [Fact]
        public async Task Private_messages_reach_only_the_two_players()
        {
            var (alice, aliceConnection) = await Connect("dm-a");
            var (bob, bobConnection) = await Connect("dm-b");
            var (_, carolConnection) = await Connect("dm-c");

            Send(alice, new SendChatCommand { CommandId = Id(), Channel = (int)ChatChannel.Private, TargetId = bob.PublicId, Text = "hi bob" }, CommandType.SendChat);
            Assert.Equal(CommandResultCode.Accepted, LastAck(aliceConnection).Result);

            ChatMessageDto Line(RecordingConnection c) => c.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).SelectMany(d => d.Changes)
                .Where(ch => ch.EntityId.StartsWith(EntityIds.ChatMessage, StringComparison.Ordinal))
                .Select(ch => TestSupport.Serializer.Deserialize<ChatMessageDto>(ch.Payload)).Last();

            var seen = Line(bobConnection);
            Assert.Equal(((int)ChatChannel.Private, "hi bob", bob.PublicId, "dm-b"), (seen.Channel, seen.Text, seen.TargetId, seen.TargetName));
            Assert.Equal("hi bob", Line(aliceConnection).Text);
            Assert.DoesNotContain(carolConnection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).SelectMany(d => d.Changes),
                ch => ch.EntityId.StartsWith(EntityIds.ChatMessage, StringComparison.Ordinal));

            Send(alice, new SendChatCommand { CommandId = Id(), Channel = (int)ChatChannel.Private, TargetId = "nobody", Text = "?" }, CommandType.SendChat);
            Assert.Equal(nameof(SocialRejectReason.UnknownPlayer), LastAck(aliceConnection).Reason);
        }
        [Fact]
        public async Task The_leader_renames_retags_and_rebanners_the_alliance()
        {
            var (leader, connection, _) = await Found("edit-a", "EDTA");
            await Found("edit-b", "EDTB");

            Send(leader, new SetAllianceSettingsCommand { CommandId = Id(), Name = "Trench Guard", Tag = "trg", Emblem = "3:5" }, CommandType.SetAllianceSettings);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var alliance = Latest<AllianceDto>(connection, EntityIds.MyAlliance);
            Assert.Equal(("Trench Guard", "TRG", "3:5"), (alliance.Name, alliance.Tag, alliance.Emblem));

            Send(leader, new SetAllianceSettingsCommand { CommandId = Id(), Tag = "EDTB" }, CommandType.SetAllianceSettings);
            Assert.Equal(nameof(SocialRejectReason.TagTaken), LastAck(connection).Reason);
            Send(leader, new SetAllianceSettingsCommand { CommandId = Id(), Emblem = "99:0" }, CommandType.SetAllianceSettings);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(connection).Reason);

        }

        [Fact]
        public async Task A_chosen_avatar_shows_in_chat_and_on_the_profile_card()
        {
            var portrait = TestSupport.Catalog.Heroes.Select(h => h.Portrait).FirstOrDefault(p => p.Length > 0) ?? "";
            var (alice, aliceConnection) = await Connect("face-a");
            var (bob, bobConnection) = await Connect("face-b");

            Send(alice, new SetProfileCommand { CommandId = Id(), AvatarId = "no-such-face" }, CommandType.SetProfile);
            Assert.Equal("UnknownAvatar", LastAck(aliceConnection).Reason);

            if (portrait.Length > 0)
            {
                Send(alice, new SetProfileCommand { CommandId = Id(), AvatarId = portrait }, CommandType.SetProfile);
                Assert.Equal(CommandResultCode.Accepted, LastAck(aliceConnection).Result);
                Assert.Equal(portrait, Latest<PlayerProfileDto>(aliceConnection, EntityIds.MyProfile).AvatarId);
            }

            Send(alice, new SendChatCommand { CommandId = Id(), Channel = (int)ChatChannel.World, Text = "hello" }, CommandType.SendChat);
            var line = bobConnection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).SelectMany(d => d.Changes)
                .Where(ch => ch.EntityId.StartsWith(EntityIds.ChatMessage, StringComparison.Ordinal))
                .Select(ch => TestSupport.Serializer.Deserialize<ChatMessageDto>(ch.Payload)).Last();
            Assert.Equal(portrait, line.SenderAvatar);

            Send(bob, new ViewProfileCommand { CommandId = Id(), PlayerId = alice.PublicId }, CommandType.ViewProfile);
            Assert.Equal(CommandResultCode.Accepted, LastAck(bobConnection).Result);
            var card = Latest<PlayerProfileDto>(bobConnection, EntityIds.Profile);
            Assert.Equal((alice.PublicId, "face-a", portrait, true), (card.PlayerId, card.Name, card.AvatarId, card.Online));

            Send(bob, new ViewProfileCommand { CommandId = Id(), PlayerId = "nobody" }, CommandType.ViewProfile);
            Assert.Equal("UnknownPlayer", LastAck(bobConnection).Reason);
        }
    }
}
