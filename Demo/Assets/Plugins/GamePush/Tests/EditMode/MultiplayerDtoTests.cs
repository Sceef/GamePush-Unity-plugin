using NUnit.Framework;
using UnityEngine;

namespace GamePush.Tests
{
    public sealed class MultiplayerDtoTests
    {
        [Test]
        public void PlayerJoinedMatchesJavascriptSdkEnvelope()
        {
            MultiplayerPlayerJoinedData joined = JsonUtility.FromJson<MultiplayerPlayerJoinedData>(
                "{\"player\":{\"playerId\":17,\"isHost\":false},\"isSelf\":true}");
            Assert.That(joined.player.playerId, Is.EqualTo(17));
            Assert.That(joined.isSelf, Is.True);
        }

        [Test]
        public void MigrationAndMessageEnvelopesKeepExternalAuthorityFields()
        {
            MultiplayerHostMigratedData migrated = JsonUtility.FromJson<MultiplayerHostMigratedData>(
                "{\"oldHost\":17,\"newHost\":21}");
            Assert.That(migrated.oldHost, Is.EqualTo(17));
            Assert.That(migrated.newHost, Is.EqualTo(21));

            MultiplayerMessageEventData message = JsonUtility.FromJson<MultiplayerMessageEventData>(
                "{\"eventName\":\"custom-event\",\"senderId\":\"21\",\"data\":\"{}\",\"timestamp\":10}");
            Assert.That(message.senderId, Is.EqualTo("21"));
            Assert.That(message.eventName, Is.EqualTo("custom-event"));
        }

        [Test]
        public void PlayerStateMapBridgeUsesTypedArrayEntries()
        {
            MultiplayerPlayerStateEntriesData states = JsonUtility.FromJson<MultiplayerPlayerStateEntriesData>(
                "{\"players\":[{\"playerId\":\"21\",\"state\":\"{\\\"ready\\\":false}\"}]}");
            Assert.That(states.players, Has.Length.EqualTo(1));
            Assert.That(states.players[0].playerId, Is.EqualTo("21"));
            Assert.That(states.players[0].state, Does.Contain("ready"));
        }
    }
}
