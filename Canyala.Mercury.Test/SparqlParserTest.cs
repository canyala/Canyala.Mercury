using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Canyala.Lagoon.Collections;
using Canyala.Lagoon.Contracts;
using Canyala.Lagoon.Extensions;
using Canyala.Lagoon.Functional;
using Canyala.Lagoon.Parsing;
using Canyala.Lagoon.Parsing.Tokenization;
using Canyala.Mercury.Rdf;
using Canyala.Mercury.Rdf.Extensions;
using Canyala.Mercury.Rdf.Internal;
using Canyala.Mercury.Storage.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Canyala.Mercury.Test
{
    [TestClass]
    public class SparqlParserTest
    {
        [TestMethod]
        public void SparqlBase()
        {
            Query query = SparqlParser.Parse("BASE <http://canyala.se/sparql#test1>");
            Assert.AreEqual("http://canyala.se/sparql#test1", query._base);
        }
    }
}
