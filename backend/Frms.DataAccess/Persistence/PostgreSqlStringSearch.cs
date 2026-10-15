using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Frms.DataAccess.Persistence;

// Shared source: both models map the same database function, without an assembly cycle.
internal static class PostgreSqlStringSearch
{
    private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo DatabaseContains = typeof(PostgreSqlStringSearch).GetMethod(nameof(Contains))!;

    internal static IQueryExpressionInterceptor QueryInterceptor { get; } = new ContainsQueryInterceptor();

    public static bool Contains(string? value, string? fragment) =>
        throw new InvalidOperationException("PostgreSQL string search must execute on the database.");

    internal static void Configure(ModelBuilder modelBuilder) => modelBuilder.HasDbFunction(DatabaseContains)
        .HasSchema("public").HasName("frms_contains_ci_as").IsNullable(false);

    private sealed class ContainsQueryInterceptor : IQueryExpressionInterceptor
    {
        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData) =>
            new ContainsVisitor().Visit(queryExpression);
    }

    private sealed class ContainsVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node) =>
            node.Method == StringContains && node.Object is not null
                ? Expression.Call(DatabaseContains, Visit(node.Object), Visit(node.Arguments[0]))
                : base.VisitMethodCall(node);
    }
}
