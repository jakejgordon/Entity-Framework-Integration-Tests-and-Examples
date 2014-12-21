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
    //All tests in this class require that you monitor the Output window (aka console) to see what queries are emitted.
    //The first three queries are related to migrations and can be ignored.
    public class LazyLoadingExamples
    {
        public const string PERSON_JANE = "Jane";
        public const string PERSON_BOB = "Bob";
        public const string PERSON_ANNA = "Anna";
        public const string PET_FLUFFY = "Fluffy";
        public const string PET_PUFFY = "Puffy";
        public const string PET_FIDO = "Fido";


        [Test]
        public void BasicQueryForOneEntity()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                Person person = dbContext.Persons.First(p => p.Name == PERSON_JANE);

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * 
                 * */
            }
        }

        [Test]
        public void UsingFromSelectWhereYieldsSameResultsAsJustDoingADotFirst()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                Person result = (from person in dbContext.Persons
                                 where person.Name == PERSON_JANE
                                 select person).First();

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * 
                 * */
            }
        }

        [Test]
        public void SelectingASingleFieldDoesntPullBackTheEntireEntity()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                string name = (from person in dbContext.Persons
                                 where person.Name == PERSON_JANE
                                 select person.Name).First();

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * */
            }
        }

        [Test]
        public void ReferencingANonIncludedEntityWillLazyLoadedViaASecondQuery()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                Person person = dbContext.Persons.First(p => p.Name == PERSON_JANE);

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * */

                Assert.That(person.Pets.First(), Is.Not.Null);

                /*
                 * SELECT 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name], 
                    [Extent1].[OwningPersonId] AS [OwningPersonId]
                    FROM [dbo].[Pet] AS [Extent1]
                    WHERE [Extent1].[OwningPersonId] = @EntityKeyValue1
                 * */
            }
        }

        [Test]
        public void ReferencingANonIncludedEntityWithLazyLoadingDisabledWillResultInANullValue()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
                dbContext.Configuration.LazyLoadingEnabled = false;

                Person person = dbContext.Persons.First(p => p.Name == PERSON_JANE);

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * */

                Assert.That(person.Pets, Is.Null);
            }
        }

        [Test]
        public void ReferencingANonIncludedEntityWithDynamicProxiesDisabledWillResultInANullValue()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
                dbContext.Configuration.ProxyCreationEnabled = false;

                Person person = dbContext.Persons.First(p => p.Name == PERSON_JANE);

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * */

                Assert.That(person.Pets, Is.Null);
            }
        }

        [Test]
        public void IncludingPetsWillCausePetsToBePulledBackAsPartOfASingleQueryForPersonsAndPets()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                Person person = dbContext.Persons
                    .Include("Pets")
                    .First(p => p.Name == PERSON_JANE);

                /*
                 * SELECT 
                    [Project1].[Id] AS [Id], 
                    [Project1].[Name] AS [Name], 
                    [Project1].[C1] AS [C1], 
                    [Project1].[Id1] AS [Id1], 
                    [Project1].[Name1] AS [Name1], 
                    [Project1].[OwningPersonId] AS [OwningPersonId]
                    FROM ( SELECT 
                        [Limit1].[Id] AS [Id], 
                        [Limit1].[Name] AS [Name], 
                        [Extent2].[Id] AS [Id1], 
                        [Extent2].[Name] AS [Name1], 
                        [Extent2].[OwningPersonId] AS [OwningPersonId], 
                        CASE WHEN ([Extent2].[Id] IS NULL) THEN CAST(NULL AS int) ELSE 1 END AS [C1]
                        FROM   (SELECT TOP (1) [Extent1].[Id] AS [Id], [Extent1].[Name] AS [Name]
                            FROM [dbo].[Person] AS [Extent1]
                            WHERE N'Jane' = [Extent1].[Name] ) AS [Limit1]
                        LEFT OUTER JOIN [dbo].[Pet] AS [Extent2] ON [Limit1].[Id] = [Extent2].[OwningPersonId]
                    )  AS [Project1]
                    ORDER BY [Project1].[Id] ASC, [Project1].[C1] ASC
                 * */

                Assert.That(person.Pets.First(), Is.Not.Null);
            }
        }

        [Test]
        public void IncludingPetsButOnlySelectingANewAnonymousObjectWithPersonAsAPropertyWillNotEagerlyLoadTheIncludedEntities()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                var result = (from person in dbContext.Persons
                                                      .Include("Pets")
                              where person.Name == PERSON_JANE
                              select new
                              {
                                  Person = person
                              }).First();

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * */

                Assert.That(result.Person.Pets.First(), Is.Not.Null);

                /*
                * SELECT 
                   [Extent1].[Id] AS [Id], 
                   [Extent1].[Name] AS [Name], 
                   [Extent1].[OwningPersonId] AS [OwningPersonId]
                   FROM [dbo].[Pet] AS [Extent1]
                   WHERE [Extent1].[OwningPersonId] = @EntityKeyValue1
                * */
            }
        }
    }
}
