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
        public void YouCanPullBackRelationshipsInAProjectionIfYouFlattenThemOut()
        {
            using (ExampleDbContext dbContext = new ExampleDbContext())
            {
                dbContext.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
                dbContext.Configuration.LazyLoadingEnabled = false;

                var result = (from person in dbContext.Persons
                              where person.Name == PERSON_JANE
                              //select all of the entities flattened out and they will be populated in their respective
                              //relationships in the object graph
                              select new
                              {
                                  Person = person,
                                  Pets = person.Pets,
                                  FavoriteFoodBrands = person.Pets.Select(p => p.FavoritePetFoodBrand)
                              }).First();

                /*
                 * SELECT 
                    [UnionAll1].[Id] AS [C1], 
                    [UnionAll1].[Id1] AS [C2], 
                    [UnionAll1].[Name] AS [C3], 
                    [UnionAll1].[C1] AS [C4], 
                    [UnionAll1].[Id2] AS [C5], 
                    [UnionAll1].[Name1] AS [C6], 
                    [UnionAll1].[OwningPersonId] AS [C7], 
                    [UnionAll1].[FavoritePetFoodBrandId] AS [C8], 
                    [UnionAll1].[C2] AS [C9], 
                    [UnionAll1].[C3] AS [C10]
                    FROM  (SELECT 
                        CASE WHEN ([Extent2].[Id] IS NULL) THEN CAST(NULL AS int) ELSE 1 END AS [C1], 
                        [Limit1].[Id] AS [Id], 
                        [Limit1].[Id] AS [Id1], 
                        [Limit1].[Name] AS [Name], 
                        [Extent2].[Id] AS [Id2], 
                        [Extent2].[Name] AS [Name1], 
                        [Extent2].[OwningPersonId] AS [OwningPersonId], 
                        [Extent2].[FavoritePetFoodBrandId] AS [FavoritePetFoodBrandId], 
                        CAST(NULL AS int) AS [C2], 
                        CAST(NULL AS varchar(1)) AS [C3]
                        FROM   (SELECT TOP (1) [Extent1].[Id] AS [Id], [Extent1].[Name] AS [Name]
                            FROM [dbo].[Person] AS [Extent1]
                            WHERE N'Jane' = [Extent1].[Name] ) AS [Limit1]
                        LEFT OUTER JOIN [dbo].[Pet] AS [Extent2] ON [Limit1].[Id] = [Extent2].[OwningPersonId]
                    UNION ALL
                        SELECT 
                        2 AS [C1], 
                        [Limit2].[Id] AS [Id], 
                        [Limit2].[Id] AS [Id1], 
                        [Limit2].[Name] AS [Name], 
                        CAST(NULL AS int) AS [C2], 
                        CAST(NULL AS varchar(1)) AS [C3], 
                        CAST(NULL AS int) AS [C4], 
                        CAST(NULL AS int) AS [C5], 
                        [Join2].[Id1] AS [Id2], 
                        [Join2].[BrandName] AS [BrandName]
                        FROM   (SELECT TOP (1) [Extent3].[Id] AS [Id], [Extent3].[Name] AS [Name]
                            FROM [dbo].[Person] AS [Extent3]
                            WHERE N'Jane' = [Extent3].[Name] ) AS [Limit2]
                        INNER JOIN  (SELECT [Extent4].[OwningPersonId] AS [OwningPersonId], [Extent5].[Id] AS [Id1], [Extent5].[BrandName] AS [BrandName]
                            FROM  [dbo].[Pet] AS [Extent4]
                            LEFT OUTER JOIN [dbo].[PetFoodBrand] AS [Extent5] ON [Extent4].[FavoritePetFoodBrandId] = [Extent5].[Id] ) AS [Join2] ON [Limit2].[Id] = [Join2].[OwningPersonId]) AS [UnionAll1]
                    ORDER BY [UnionAll1].[Id1] ASC, [UnionAll1].[C1] ASC
                 * */

                Assert.That(result.Person, Is.Not.Null);
                Assert.That(result.Person.Pets, Is.Not.Null);
                Assert.That(result.Pets, Is.Not.Null);
                Assert.That(result.FavoriteFoodBrands, Is.Not.Null);
                Assert.That(result.Person.Pets.Select(pet => pet.FavoritePetFoodBrand).Count(), Is.GreaterThan(0));
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
