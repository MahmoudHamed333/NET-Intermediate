using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Collections.Generic;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;
        private readonly List<string> _statements;
        private bool _collectingStatements;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
            _statements = new List<string>();
            _collectingStatements = false;
        }

        public string Translate(Expression exp)
        {
            Visit(exp);
            return _resultStringBuilder.ToString();
        }

        public List<string> GetStatements(Expression exp)
        {
            _collectingStatements = true;
            _statements.Clear();
            Visit(exp);
            return _statements;
        }

        #region protected methods
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);
                return node;
            }

            // Handle String methods: StartsWith, EndsWith, Contains, Equals
            if (node.Method.DeclaringType == typeof(string))
            {
                switch (node.Method.Name)
                {
                    case "StartsWith":
                        Visit(node.Object);
                        _resultStringBuilder.Append("(");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append("*)");
                        return node;

                    case "EndsWith":
                        Visit(node.Object);
                        _resultStringBuilder.Append("(*");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append(")");
                        return node;

                    case "Contains":
                        Visit(node.Object);
                        _resultStringBuilder.Append("(*");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append("*)");
                        return node;

                    case "Equals":
                        Visit(node.Object);
                        _resultStringBuilder.Append("(");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append(")");
                        return node;
                }
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    HandleEqualityOperation(node);
                    break;

                case ExpressionType.AndAlso:
                    if (_collectingStatements)
                    {
                        // For AND operations, collect each part as separate statements
                        var leftTranslator = new ExpressionToFtsRequestTranslator();
                        var leftResult = leftTranslator.Translate(node.Left);
                        _statements.Add(leftResult);

                        var rightTranslator = new ExpressionToFtsRequestTranslator();
                        var rightResult = rightTranslator.Translate(node.Right);
                        _statements.Add(rightResult);
                    }
                    else
                    {
                        // For regular translation, just visit both sides
                        Visit(node.Left);
                        Visit(node.Right);
                    }
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            }
            return node;
        }

        private void HandleEqualityOperation(BinaryExpression node)
        {
            Expression memberExpression = null;
            Expression constantExpression = null;

            // Handle both orders: member == constant and constant == member
            if (node.Left.NodeType == ExpressionType.MemberAccess &&
                node.Right.NodeType == ExpressionType.Constant)
            {
                memberExpression = node.Left;
                constantExpression = node.Right;
            }
            else if (node.Left.NodeType == ExpressionType.Constant &&
                     node.Right.NodeType == ExpressionType.MemberAccess)
            {
                constantExpression = node.Left;
                memberExpression = node.Right;
            }
            else
            {
                throw new NotSupportedException("Equality operation requires one member access and one constant");
            }

            Visit(memberExpression);
            _resultStringBuilder.Append("(");
            Visit(constantExpression);
            _resultStringBuilder.Append(")");
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");
            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);
            return node;
        }
        #endregion
    }
}