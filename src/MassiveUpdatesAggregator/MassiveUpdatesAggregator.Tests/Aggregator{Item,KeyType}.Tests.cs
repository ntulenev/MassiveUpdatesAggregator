using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MassiveUpdatesAggregator.Tests
{
    public class TestItem : IAggregatorItem<object>
    {
        public object Key => string.Empty;
    }

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
    }
}
