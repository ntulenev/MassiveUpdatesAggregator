using FluentAssertions;
using Moq;
using System;
using System.Threading;
using Xunit;

namespace MassiveUpdatesAggregator.Tests
{
    public class TestItem : IAggregatorItem<object>
    {
        public object Key => throw new System.NotImplementedException();
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
            var exception = Record.Exception(() => aggregator =  new Aggregator<TestItem, object>(size, delay, strategy, CancellationToken.None));

            // Assert
            exception.Should().BeNull();
            aggregator.IsRunning.Should().BeTrue();
        }
    }
}
