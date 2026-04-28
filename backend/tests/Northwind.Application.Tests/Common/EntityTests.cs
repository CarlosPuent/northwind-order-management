using AwesomeAssertions;
using Northwind.Domain.Common;
using Xunit;

namespace Northwind.Application.Tests.Common;

public class EntityTests
{
    private class TestEntity : Entity<int>
    {
        public TestEntity(int id) : base(id) { }
    }

    [Fact]
    public void TwoEntities_WithSameId_ShouldBeEqual()
    {
        var a = new TestEntity(1);
        var b = new TestEntity(1);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void TwoEntities_WithDifferentIds_ShouldNotBeEqual()
    {
        var a = new TestEntity(1);
        var b = new TestEntity(2);
        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Entity_ShouldNotEqualNull()
    {
        var entity = new TestEntity(1);
        (entity == null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeConsistentWithId()
    {
        var a = new TestEntity(42);
        var b = new TestEntity(42);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}