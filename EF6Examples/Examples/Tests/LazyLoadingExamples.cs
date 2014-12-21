using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Examples.Models;
using NUnit.Framework;

namespace Examples.Tests
{
    [TestFixture]
    public class LazyLoadingExamples
    {
        [Test]
        public void BasicQueryForOneEntity()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                //Person person = dbContext.Persons.F
            }
        }
    }
}
