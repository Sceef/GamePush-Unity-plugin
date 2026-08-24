using NUnit.Framework;
using UnityEngine;

namespace GamePush.Tests
{
    public sealed class FeedbacksAndReactionsTests
    {
        [Test]
        public void FeedbackDataRoundtripsUnityJsonContract()
        {
            FeedbackData feedback = JsonUtility.FromJson<FeedbackData>(
                "{\"id\":\"14444\",\"type\":\"ERROR\",\"text\":\"broken\",\"status\":\"NEW\",\"files\":[\"https://example.com/a.png\"],\"messages\":[{\"id\":\"1\",\"text\":\"hello\",\"author\":\"PLAYER\",\"feedbackId\":\"14444\"}],\"playerId\":12}");

            Assert.That(feedback.id, Is.EqualTo("14444"));
            Assert.That(feedback.type, Is.EqualTo("ERROR"));
            Assert.That(feedback.status, Is.EqualTo("NEW"));
            Assert.That(feedback.files, Has.Length.EqualTo(1));
            Assert.That(feedback.messages, Has.Length.EqualTo(1));
            Assert.That(feedback.messages[0].author, Is.EqualTo("PLAYER"));
            Assert.That(typeof(FeedbackMessageData), Is.Not.Null);
        }

        [Test]
        public void FeedbacksFetchResultKeepsCanLoadMore()
        {
            FeedbacksFetchResult result = JsonUtility.FromJson<FeedbacksFetchResult>(
                "{\"items\":[{\"id\":\"2\",\"type\":\"SUGGESTION\",\"text\":\"more gold\",\"status\":\"IN_PROGRESS\"}],\"canLoadMore\":true}");

            Assert.That(result.canLoadMore, Is.True);
            Assert.That(result.items, Has.Length.EqualTo(1));
            Assert.That(result.items[0].type, Is.EqualTo("SUGGESTION"));
        }

        [Test]
        public void ReactionResultAndFileReactionsParseFromBridgeJson()
        {
            ReactionResult reaction = JsonUtility.FromJson<ReactionResult>(
                "{\"entityType\":\"FILE\",\"entityId\":\"12333\",\"reactionType\":\"like\",\"counter\":4}");
            Assert.That(reaction.entityType, Is.EqualTo("FILE"));
            Assert.That(reaction.counter, Is.EqualTo(4));

            FileData file = JsonUtility.FromJson<FileData>(
                "{\"id\":\"12333\",\"playerId\":1,\"name\":\"shot.png\",\"src\":\"https://example.com/shot.png\",\"size\":10,\"tags\":[],\"reactions\":[{\"type\":\"like\",\"count\":4}],\"playerReactions\":[{\"reactionType\":\"like\"}]}");
            Assert.That(file.reactions, Has.Length.EqualTo(1));
            Assert.That(file.reactions[0].count, Is.EqualTo(4));
            Assert.That(file.playerReactions[0].reactionType, Is.EqualTo("like"));
        }

        [Test]
        public void ConfirmWindowDataKeepsHideCancelButton()
        {
            ConfirmWindowData data = new ConfirmWindowData("Drop", "Sure?", "Yes", "No", true, true);
            Assert.That(data.hideCancelButton, Is.True);
            Assert.That(data.invertButtonColors, Is.True);
        }
    }
}
