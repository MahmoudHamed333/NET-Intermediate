using System;
using System.Linq;
using System.Linq.Expressions;
using Expressions.Task3.E3SQueryProvider.Models.Entities;
using Xunit;

namespace Expressions.Task3.E3SQueryProvider.Test
{
    public class E3SAndOperatorSupportTests
    {
        #region SubTask 3: AND operator support
        [Fact]
        public void TestAndQueryable()
        {
            var translator = new ExpressionToFtsRequestTranslator();
            Expression<Func<IQueryable<EmployeeEntity>, IQueryable<EmployeeEntity>>> expression
                = query => query.Where(e => e.Workstation == "EPRUIZHW006" && e.Manager.StartsWith("John"));

            // Get statements for AND operation
            var statements = translator.GetStatements(expression);

            // Assert that we have two statements
            Assert.Equal(2, statements.Count);
            Assert.Contains("Workstation:(EPRUIZHW006)", statements);
            Assert.Contains("Manager:(John*)", statements);
        }
        #endregion
    }
}