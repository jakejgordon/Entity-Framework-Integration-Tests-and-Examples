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
    //The first three queries are related to migrations and can be ignored. The resulting queries are displayed immediately
    //after the code that causes them to be emitted below.
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
        public void ReferencingANonIncludedPropertyStillRetrievesThatProperty()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                List<Pet> result = (from person in dbContext.Persons
                                 where person.Name == PERSON_JANE
                                 //select Pets, even though it wasn't explicitly Included
                                 select person.Pets)
                                    .First();

                /*
                 * 
                    SELECT 
                        [Project1].[Id] AS [Id], 
                        [Project1].[C1] AS [C1], 
                        [Project1].[Id1] AS [Id1], 
                        [Project1].[Name] AS [Name], 
                        [Project1].[OwningPersonId] AS [OwningPersonId]
                        FROM ( SELECT 
                            [Limit1].[Id] AS [Id], 
                            [Extent2].[Id] AS [Id1], 
                            [Extent2].[Name] AS [Name], 
                            [Extent2].[OwningPersonId] AS [OwningPersonId], 
                            CASE WHEN ([Extent2].[Id] IS NULL) THEN CAST(NULL AS int) ELSE 1 END AS [C1]
                            FROM   (SELECT TOP (1) [Extent1].[Id] AS [Id]
                                FROM [dbo].[Person] AS [Extent1]
                                WHERE N'Jane' = [Extent1].[Name] ) AS [Limit1]
                            LEFT OUTER JOIN [dbo].[Pet] AS [Extent2] ON [Limit1].[Id] = [Extent2].[OwningPersonId]
                        )  AS [Project1]
                        ORDER BY [Project1].[Id] ASC, [Project1].[C1] ASC

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
                    [Extent1].[Name] AS [Name]       -- only the Name field is selected
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

                //the below query isn't emitted until the call to person.Pets.First() as it is lazy loaded
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

                //since Pets are not eagerly loaded and lazy loading is disabled, this will be null
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

                //since Pets are not eagerly loaded and proxy creation is disabled, this will be null
                Assert.That(person.Pets, Is.Null);
            }
        }

        [Test]
        public void ReferencingANonIncludedRelatedEntityThatWasAlreadyLoadedOnThisDbContextWillReturnANonNullValueBecauseItIsAlreadyCachedUpEvenWithLazyLoadingDisabled()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
                dbContext.Configuration.LazyLoadingEnabled = false;

                //fetch all pets so they are cached up on the dbContext already
                List<Pet> pets = dbContext.Pets.ToList();

                /*
                 * SELECT 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name], 
                    [Extent1].[OwningPersonId] AS [OwningPersonId]
                    FROM [dbo].[Pet] AS [Extent1]
                 * */

                Person person = dbContext.Persons.First(p => p.Name == PERSON_JANE);

                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * */

                //no query is emitted because the Pets property is already fully loaded as it was already cached on the DbContext
                Assert.That(person.Pets, Is.Not.Null);
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
                              //rather than just selecting 'person', creating a new anonynous type with Person as a property
                              //will not pull back the Included Pet entities
                              select new
                              {
                                  Person = person
                              }).First();

                //nothing related to Pets is eagerly loaded
                /*
                 * SELECT TOP (1) 
                    [Extent1].[Id] AS [Id], 
                    [Extent1].[Name] AS [Name]
                    FROM [dbo].[Person] AS [Extent1]
                    WHERE N'Jane' = [Extent1].[Name]
                 * */

                Assert.That(result.Person.Pets.First(), Is.Not.Null);

                //Pets are still lazy loaded
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
        public void YouCanCherryPickFieldsFromRelatedEntitiesWithoutUsingInclude()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

                var result = (from person in dbContext.Persons
                              where person.Name == PERSON_JANE
                              //create a result set that is just the person name with the names of the pets. No Include is necessary.
                              select new
                              {
                                  PersonName = person.Name,
                                  PetNames = person.Pets.Select(pet => pet.Name)
                              }).First();

                /*
                 * SELECT 
                    [Project1].[Id] AS [Id], 
                    [Project1].[Name] AS [Name], 
                    [Project1].[C1] AS [C1], 
                    [Project1].[Name1] AS [Name1]
                    FROM ( SELECT 
                        [Limit1].[Id] AS [Id], 
                        [Limit1].[Name] AS [Name], 
                        [Extent2].[Name] AS [Name1], 
                        CASE WHEN ([Extent2].[OwningPersonId] IS NULL) THEN CAST(NULL AS int) ELSE 1 END AS [C1]
                        FROM   (SELECT TOP (1) [Extent1].[Id] AS [Id], [Extent1].[Name] AS [Name]
                            FROM [dbo].[Person] AS [Extent1]
                            WHERE N'Jane' = [Extent1].[Name] ) AS [Limit1]
                        LEFT OUTER JOIN [dbo].[Pet] AS [Extent2] ON [Limit1].[Id] = [Extent2].[OwningPersonId]
                    )  AS [Project1]
                    ORDER BY [Project1].[Id] ASC, [Project1].[C1] ASC
                 * */

                Assert.That(result.PersonName, Is.Not.Null);
                Assert.That(result.PetNames, Is.Not.Null);
            }
        }
    }
}
