using Microsoft.EntityFrameworkCore;
using Moq;

namespace ITS.UnitTests.Helpers;

internal static class MockDbSetFactory
{
    /// <summary>
    /// Creates a Moq DbSet that supports async LINQ operators (FirstOrDefaultAsync, ToListAsync, etc.)
    /// over the provided in-memory data. Include() calls are no-ops — pre-populate navigation
    /// properties on the test objects before passing them in.
    /// </summary>
    public static Mock<DbSet<T>> Create<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mock = new Mock<DbSet<T>>();

        mock.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mock.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mock.As<IQueryable<T>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mock.As<IQueryable<T>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mock.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => queryable.GetEnumerator());

        return mock;
    }
}
