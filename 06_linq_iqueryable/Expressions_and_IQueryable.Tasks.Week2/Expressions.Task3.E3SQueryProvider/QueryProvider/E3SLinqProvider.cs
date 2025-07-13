using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;

namespace Expressions.Task3.E3SQueryProvider
{
    public class E3SLinqProvider<T> : IQueryProvider
    {
        private readonly FtsRequestGenerator _requestGenerator;
        private readonly string _baseAddress;

        public E3SLinqProvider(string baseAddress)
        {
            _baseAddress = baseAddress;
            _requestGenerator = new FtsRequestGenerator(baseAddress);
        }
        public E3SLinqProvider()
        {
            
        }
        public IQueryable CreateQuery(Expression expression)
        {
            return new E3SQueryable<T>(this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new E3SQueryable<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return Execute<T>(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var translator = new ExpressionToFtsRequestTranslator();

            // Check if this is an AND operation that needs multiple statements
            var statements = translator.GetStatements(expression);

            Uri requestUri;
            if (statements.Count > 1)
            {
                // Multiple statements for AND operation
                requestUri = _requestGenerator.GenerateRequestUrl<T>(statements);
            }
            else
            {
                // Single statement
                var query = translator.Translate(expression);
                requestUri = _requestGenerator.GenerateRequestUrl<T>(query);
            }

            if (typeof(TResult) == typeof(string))
            {
                return (TResult)(object)requestUri.ToString();
            }

            throw new NotImplementedException("Actual HTTP request execution not implemented for testing purposes");
        }
    }

    public class E3SQueryable<T> : IQueryable<T>
    {
        private readonly IQueryProvider _provider;
        private readonly Expression _expression;

        public E3SQueryable(IQueryProvider provider, Expression expression)
        {
            _provider = provider;
            _expression = expression;
        }

        public E3SQueryable(IQueryProvider provider)
        {
            _provider = provider;
            _expression = Expression.Constant(this);
        }

        public Type ElementType => typeof(T);

        public Expression Expression => _expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator()
        {
            var result = _provider.Execute<IEnumerable<T>>(_expression);
            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // Extension methods for easier usage
    public static class E3SQueryableExtensions
    {
        public static IQueryable<T> AsE3SQueryable<T>(this IQueryable<T> source, string baseAddress)
        {
            var provider = new E3SLinqProvider<T>(baseAddress);
            return new E3SQueryable<T>(provider);
        }
    }
}