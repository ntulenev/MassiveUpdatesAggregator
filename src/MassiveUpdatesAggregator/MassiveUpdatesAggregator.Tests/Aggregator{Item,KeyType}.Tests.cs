using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Moq;

using Xunit;

namespace MassiveUpdatesAggregator.Tests
{
    public class AggregatorTests
    {
        [Theory(DisplayName = "Aggregator can't be created with wrong size.")]
        [InlineData(0)]
        [InlineData(-1)]
        [Trait("Category", "Unit")]
        public void CantCreateAggregatorWithWrongSize(int size)
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var delay = 1000;

            // Act
            var exception = Record.Exception(() => new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None));

            // Assert
            exception.Should().NotBeNull().And.BeOfType<ArgumentException>();
        }

        [Theory(DisplayName = "Aggregator can't be created with wrong delay.")]
        [InlineData(0)]
        [InlineData(-1)]
        [Trait("Category", "Unit")]
        public void CantCreateAggregatorWithWrongDelay(int delay)
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var size = 5;

            // Act
            var exception = Record.Exception(() => new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None));

            // Assert
            exception.Should().NotBeNull().And.BeOfType<ArgumentException>();
        }


        [Fact(DisplayName = "Aggregator can't be created with not set strategy.")]
        [Trait("Category", "Unit")]
        public void CantCreateAggregatorWithNullStrategy()
        {
            // Arrange
            IAggregationStrategy<TestItem, object> strategy = null!;
            var size = 5;
            var delay = 1000;

            // Act
            var exception = Record.Exception(() => new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None));

            // Assert
            exception.Should().NotBeNull().And.BeOfType<ArgumentNullException>();
        }

        [Fact(DisplayName = "Aggregator is running after created.")]
        [Trait("Category", "Unit")]
        public void AggregatorIsRunningAfterCreated()
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var size = 5;
            var delay = 1000;
            Aggregator<TestItem, object> aggregator = null!;

            // Act
            var exception = Record.Exception(() => aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None));

            // Assert
            exception.Should().BeNull();
            aggregator.IsRunning.Should().BeTrue();
        }

        [Fact(DisplayName = "Aggregator is not running after stop.")]
        [Trait("Category", "Unit")]
        public async Task AggregatorNotIsRunningAfterStoppedAsync()
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var size = 5;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);

            // Act
            var exception = await Record.ExceptionAsync(async () => await aggregator.StopAsync().ConfigureAwait(false));

            // Assert
            exception.Should().BeNull();
            aggregator.IsRunning.Should().BeFalse();
        }

        [Fact(DisplayName = "Aggregator cant be stopped twice.")]
        [Trait("Category", "Unit")]
        public async Task AggregatorErrorOnDoubleStop()
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var size = 5;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);
            await aggregator.StopAsync().ConfigureAwait(false);

            // Act
            var exception = await Record.ExceptionAsync(async () => await aggregator.StopAsync().ConfigureAwait(false));

            // Assert
            exception.Should().NotBeNull().And.BeOfType<InvalidOperationException>();
        }

        [Fact(DisplayName = "Aggregator could not accept null message.")]
        [Trait("Category", "Unit")]
        public async Task MessageCouldNotBeSendIfNull()
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var size = 5;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);
            TestItem item = null!;

            // Act
            var exception = await Record.ExceptionAsync(async () => await aggregator.SendAsync(item));

            // Assert
            exception.Should().NotBeNull().And.BeOfType<ArgumentNullException>();
        }

        [Fact(DisplayName = "Aggregator could send message.")]
        [Trait("Category", "Unit")]
        public async Task MessageCouldBeSended()
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var size = 1;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);
            TestItem item = new TestItem();

            // Act
            var exception = await Record.ExceptionAsync(async () => await aggregator.SendAsync(item));

            // Assert
            exception.Should().BeNull();
        }

        [Fact(DisplayName = "Aggregator does not get message before delay.")]
        [Trait("Category", "Unit")]
        public async Task AggregatorDoesNotGetMessageBeforeDelay()
        {
            // Arrange
            var strategy = (new Mock<IAggregationStrategy<TestItem, object>>()).Object;
            var size = 1;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);
            TestItem item = new TestItem();
            await aggregator.SendAsync(item);

            // Act
            var isItemReadyTask = aggregator.GetAsyncEnumerator().MoveNextAsync();

            // Assert
            isItemReadyTask.IsCompleted.Should().BeFalse();
        }

        [Fact(DisplayName = "Aggregator gets message after delay.")]
        [Trait("Category", "Unit")]
        public async Task AggregatorGetsMessageAfterDelay()
        {
            var item = new TestItem();
            var resultItem = new TestItem();

            // Arrange
            var aggregatorMock = (new Mock<IAggregationStrategy<TestItem, object>>());
            var strategy = aggregatorMock.Object;

            aggregatorMock.Setup(x => x.Merge(It.Is<IEnumerable<TestItem>>(coll => coll.Single() == item))).Returns(resultItem);

            var size = 1;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);

            await aggregator.SendAsync(item);

            // Act
            var enumerator = aggregator.GetAsyncEnumerator();
            var isItemReadyTask = enumerator.MoveNextAsync();

            // Assert
            await Task.Delay(2000).ConfigureAwait(false); //Attemts to wait aggregator timeout
            isItemReadyTask.IsCompleted.Should().BeTrue();
            var res = await isItemReadyTask;
            res.Should().BeTrue();
            enumerator.Current.Should().Be(resultItem);
        }

        [Fact(DisplayName = "Aggregator with 2 messages gets message after delay.")]
        [Trait("Category", "Unit")]
        public async Task AggregatorWith2MsgsGetsMessageAfterDelay()
        {
            var item1 = new TestItem();
            var item2 = new TestItem();
            var resultItem = new TestItem();

            // Arrange
            var aggregatorMock = (new Mock<IAggregationStrategy<TestItem, object>>());
            var strategy = aggregatorMock.Object;

            aggregatorMock.Setup(x => x.Merge(It.Is<IEnumerable<TestItem>>(coll => coll.Count() == 2 && coll.First() == item1 && coll.Last() == item2))).Returns(resultItem);

            var size = 1;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);

            await aggregator.SendAsync(item1);
            await Task.Delay(100).ConfigureAwait(false); //Attemts to emulate some delay between messages less then aggregator delay
            await aggregator.SendAsync(item2);

            // Act
            var enumerator = aggregator.GetAsyncEnumerator();
            var isItemReadyTask = enumerator.MoveNextAsync();

            // Assert
            await Task.Delay(2000).ConfigureAwait(false); //Attemts to wait aggregator timeout
            isItemReadyTask.IsCompleted.Should().BeTrue();
            var res = await isItemReadyTask;
            res.Should().BeTrue();
            enumerator.Current.Should().Be(resultItem);
        }

        [Fact(DisplayName = "Aggregator with 2 messages and timeout gets message after delay.")]
        [Trait("Category", "Unit")]
        public async Task AggregatorWith2MsgsAndTimeoutGetsMessageAfterDelay()
        {
            var item1 = new TestItem();
            var item2 = new TestItem();
            var resultItem = new TestItem();

            // Arrange
            var aggregatorMock = (new Mock<IAggregationStrategy<TestItem, object>>());
            var strategy = aggregatorMock.Object;

            aggregatorMock.Setup(x => x.Merge(It.Is<IEnumerable<TestItem>>(coll => coll.Single() == item1))).Returns(resultItem);

            var size = 1;
            var delay = 1000;
            var aggregator = new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None);

            await aggregator.SendAsync(item1);
            await Task.Delay(2000).ConfigureAwait(false); //Attemts to emulate long delay between messages more then aggregator delay
            await aggregator.SendAsync(item2);

            // Act
            var enumerator = aggregator.GetAsyncEnumerator();
            var isItemReadyTask = enumerator.MoveNextAsync();

            // Assert
            isItemReadyTask.IsCompleted.Should().BeTrue();
            var res = await isItemReadyTask;
            res.Should().BeTrue();
            enumerator.Current.Should().Be(resultItem);
        }
    }
}
