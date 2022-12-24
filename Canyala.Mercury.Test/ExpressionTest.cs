//
// Copyright (c) 2013 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class ExpressionTest
    {
        [TestMethod]
        public void TestExpressionWithReturnParamAndDifferentTypes()
        {
            var text = "2";
            var value = int.Parse(text);
            var param = Expression.Parameter(typeof(double));
            var body = Expression.Multiply(param, Expression.Convert(Expression.Constant(value), typeof(double)));
            var expr = Expression.Lambda<Func<double, double>>(body, param);
            var compute = expr.Compile();
            var result = compute(5.0);

            Assert.AreEqual(10.0, result);
        }
    }
}
