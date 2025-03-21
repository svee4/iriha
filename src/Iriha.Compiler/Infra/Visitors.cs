using Iriha.Compiler.Parsing.Nodes;
using System.Diagnostics;

namespace Iriha.Compiler.Infra;

public static class Visitors
{
	public sealed class ExpressionVisitor<T>
	{
		public required Func<FunctionCallExpression, ExpressionVisitor<T>, T> FunctionCallExpressionVisitor { get; init; }
		public required Func<IndexerCallExpression, ExpressionVisitor<T>, T> IndexerCallExpressionVisitor { get; init; }

		public required Func<VariableDeclarationStatement, ExpressionVisitor<T>, T> VariableDeclarationStatementVisitor { get; init; }
		public required Func<AssignmentExpression, ExpressionVisitor<T>, T> AssignmentExpressionVisitor { get; init; }
		public required Func<DiscardExpression, ExpressionVisitor<T>, T> DiscardExpressionVisitor { get; init; }

		public required Func<MemberAccessExpression, ExpressionVisitor<T>, T> MemberAccessExpressionVisitor { get; init; }
		public required Func<IdentifierExpression, ExpressionVisitor<T>, T> IdentifierExpressionVisitor { get; init; }

		public required Func<BlockExpression, ExpressionVisitor<T>, T> BlockExpressionVisitor { get; init; }
		public required Func<ReturnExpression, ExpressionVisitor<T>, T> ReturnExpressionVisitor { get; init; }
		public required Func<YieldExpression, ExpressionVisitor<T>, T> YieldExpressionVisitor { get; init; }

		public required Func<StringLiteralExpression, ExpressionVisitor<T>, T> StringLiteralExpressionVisitor { get; init; }
		public required Func<IntLiteralExpression, ExpressionVisitor<T>, T> IntLiteralExpressionVisitor { get; init; }

		public required Func<AdditionExpression, ExpressionVisitor<T>, T> AdditionExpressionVisitor { get; init; }
		public required Func<SubtractionExpression, ExpressionVisitor<T>, T> SubtractionExpressionVisitor { get; init; }
		public required Func<MultiplicationExpression, ExpressionVisitor<T>, T> MultiplicationExpressionVisitor { get; init; }
		public required Func<DivisionExpression, ExpressionVisitor<T>, T> DivisionExpressionVisitor { get; init; }

		public required Func<BitwiseAndExpression, ExpressionVisitor<T>, T> BitwiseAndExpressionVisitor { get; init; }
		public required Func<BitwiseOrExpression, ExpressionVisitor<T>, T> BitwiseOrExpressionVisitor { get; init; }
		public required Func<BitwiseXorExpression, ExpressionVisitor<T>, T> BitwiseXorExpressionVisitor { get; init; }
		public required Func<BitwiseLeftShiftExpression, ExpressionVisitor<T>, T> BitwiseLeftShiftExpressionVisitor { get; init; }
		public required Func<BitwiseRightShiftExpression, ExpressionVisitor<T>, T> BitwiseRightShiftExpressionVisitor { get; init; }

		public required Func<LogicalAndExpression, ExpressionVisitor<T>, T> LogicalAndExpressionVisitor { get; init; }
		public required Func<LogicalOrExpression, ExpressionVisitor<T>, T> LogicalOrExpressionVisitor { get; init; }
		public required Func<LogicalLessThanExpression, ExpressionVisitor<T>, T> LogicalLessThanExpressionVisitor { get; init; }
		public required Func<LogicalGreaterThanExpression, ExpressionVisitor<T>, T> LogicalGreaterThanExpressionVisitor { get; init; }
		public required Func<LogicalEqualExpression, ExpressionVisitor<T>, T> LogicalEqualExpressionVisitor { get; init; }
		public required Func<LogicalNotEqualExpression, ExpressionVisitor<T>, T> LogicalNotEqualExpressionVisitor { get; init; }

		public required Func<LogicalNegationExpression, ExpressionVisitor<T>, T> LogicalNegationExpressionVisitor { get; init; }
		public required Func<MathematicalNegationExpression, ExpressionVisitor<T>, T> MathematicalNegationExpressionVisitor { get; init; }
		public required Func<BitwiseNotExpression, ExpressionVisitor<T>, T> BitwiseNotExpressionVisitor { get; init; }
		public required Func<DereferenceExpression, ExpressionVisitor<T>, T> DereferenceExpressionVisitor { get; init; }

		public T BinaryVisit(BinaryExpression expr, Func<T, T, T> func) =>
			func(Visit(expr.Left), Visit(expr.Right));

		public T Visit(IExpression expression) =>
			expression switch
			{
				FunctionCallExpression ex => FunctionCallExpressionVisitor(ex, this),
				IndexerCallExpression ex => IndexerCallExpressionVisitor(ex, this),

				VariableDeclarationStatement ex => VariableDeclarationStatementVisitor(ex, this),
				AssignmentExpression ex => AssignmentExpressionVisitor(ex, this),
				DiscardExpression ex => DiscardExpressionVisitor(ex, this),

				MemberAccessExpression ex => MemberAccessExpressionVisitor(ex, this),
				IdentifierExpression ex => IdentifierExpressionVisitor(ex, this),

				BlockExpression ex => BlockExpressionVisitor(ex, this),
				ReturnExpression ex => ReturnExpressionVisitor(ex, this),
				YieldExpression ex => YieldExpressionVisitor(ex, this),

				StringLiteralExpression ex => StringLiteralExpressionVisitor(ex, this),
				IntLiteralExpression ex => IntLiteralExpressionVisitor(ex, this),

				AdditionExpression ex => AdditionExpressionVisitor(ex, this),
				SubtractionExpression ex => SubtractionExpressionVisitor(ex, this),
				MultiplicationExpression ex => MultiplicationExpressionVisitor(ex, this),
				DivisionExpression ex => DivisionExpressionVisitor(ex, this),

				BitwiseAndExpression ex => BitwiseAndExpressionVisitor(ex, this),
				BitwiseOrExpression ex => BitwiseOrExpressionVisitor(ex, this),
				BitwiseXorExpression ex => BitwiseXorExpressionVisitor(ex, this),
				BitwiseLeftShiftExpression ex => BitwiseLeftShiftExpressionVisitor(ex, this),
				BitwiseRightShiftExpression ex => BitwiseRightShiftExpressionVisitor(ex, this),

				LogicalAndExpression ex => LogicalAndExpressionVisitor(ex, this),
				LogicalOrExpression ex => LogicalOrExpressionVisitor(ex, this),
				LogicalLessThanExpression ex => LogicalLessThanExpressionVisitor(ex, this),
				LogicalGreaterThanExpression ex => LogicalGreaterThanExpressionVisitor(ex, this),
				LogicalEqualExpression ex => LogicalEqualExpressionVisitor(ex, this),
				LogicalNotEqualExpression ex => LogicalNotEqualExpressionVisitor(ex, this),

				LogicalNegationExpression ex => LogicalNegationExpressionVisitor(ex, this),
				MathematicalNegationExpression ex => MathematicalNegationExpressionVisitor(ex, this),
				BitwiseNotExpression ex => BitwiseNotExpressionVisitor(ex, this),
				DereferenceExpression ex => DereferenceExpressionVisitor(ex, this),

				{ } ex => throw new UnreachableException($"Unhandled expression of type {ex.GetType()}"),
				null => throw new ArgumentNullException(nameof(expression)),
			};
	}
}
