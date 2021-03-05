
[![NuGet](https://img.shields.io/nuget/v/Ardalis.Specification.svg)](https://www.nuget.org/packages/Ardalis.Specification)[![NuGet](https://img.shields.io/nuget/dt/Ardalis.Specification.svg)](https://www.nuget.org/packages/Ardalis.Specification)
[![Actions Status](https://github.com/ardalis/specification/workflows/ASP.NET%20Core%20CI/badge.svg)](https://github.com/ardalis/specification/actions)
[![Generic badge](https://img.shields.io/badge/Documentation-Ardalis.Specification%20v5-Green.svg)](https://ardalis.github.io/Specification/)

[![Stars Sparkline](https://stars.medv.io/ardalis/specification.svg)](https://stars.medv.io/ardalis/specification)

# Specification

Base class with tests for adding specifications to a DDD model. Currently used in Microsoft reference application [eShopOnWeb](https://github.com/dotnet-architecture/eShopOnWeb), which is the best place to see it in action. Check out Steve "ardalis" Smith's associated (free!) eBook, [Architecting Modern Web Applications with ASP.NET Core and Azure](https://aka.ms/webappebook), as well.

[Read the Docs on GitHub Pages](https://ardalis.github.io/Specification/)

🎥 [Watch What's New in v5 of Ardalis.Specification](https://www.youtube.com/watch?v=gT72mWdD4Qo&ab_channel=Ardalis)

🎥 [Watch an Overview of the Pattern and this Package](https://www.youtube.com/watch?v=BgWWbBUWyig)

## Give a Star! :star:
If you like or are using this project please give it a star. Thanks!

## Sample Usage

The Specification pattern pulls query-specific logic out of other places in the application where it currently exists. For applications with minimal abstraction that use EF Core directly, the specification will eliminate `Where`, `Include`, `Select` and similar expressions from almost all places where they're being used. In applications that abstract database query logic behind a `Repository` abstraction, the specification will typically eliminate the need for many custom `Repository` implementation classes as well as custom query methods on `Repository` implementations. Instead of many different ways to filter and shape data using various methods, the same capability is achieved with just this code:

```csharp
public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
{
  var specificationResult = await ApplySpecification(spec);
  return await specificationResult.ToListAsync();
}
private async Task<IQueryable<T>> ApplySpecification(ISpecification<T> spec)
{
  return await EfSpecificationEvaluator<T>.GetQuery(_dbContext.Set<T>().AsQueryable(), spec);
}
```

Now to use this method, the calling code simply instantiates the appropriate specification implementation. Specifications should be defined in an easily-discovered location in the application, so developers can easily reuse them. The use of this pattern helps to eliminate many commonly duplicated lambda expressions in applications, reducing bugs associated with this duplication.

## Running the tests

This project needs a database to test, since a lot of the tests validate that a specification is translated from LINQ to SQL by EF Core. To run the tests, we're using docker containers, including a docker-hosted SQL Server instance. You run the tests by simply running `RunTests.bat` or `RunTests.sh`.

## Frequently Asked Questions

### Does the use of filters break the Open-Closed Principle?

Filters are an optional approach to use with specifications. You'll find samples of them [here](https://github.com/ardalis/Specification/tree/master/sample/Ardalis.SampleApp.Core/Specifications/Filters).

This is a totally valid question. If the intention by using the specifications is to conform to this principle, then by adding the concept of filters, aren't we doing the opposite? We go back and update the specification by adding additional conditions.
As a brief recap, the OCP predicates that we should have constructs that are open to extension and closed to changes. This means, if we need to add a "behavior" to a class, we should be able to do that without changing the class itself. Even more simplified, if you have switch statements and too many conditional logic; it might be a sign that the behavior is too much hardcoded, and might be refactored in a better way.

In our case, indeed we have too many conditions within the specification, so this concern is partially true. The catch here is that we have to do with single and atomic business requirement. The user has demanded from us that we change the behavior and add an additional condition by which the customers can be queried. The requirement is that the filters on the customer's UI page be extended. Whenever you have business requirement changes, undoubtedly you will have code changes in the domain model as well. That particular specification will change only when this exact business requirement changes, and never otherwise; it's wired up to this functionality only. Even though not the perfect structure, I might say it's an acceptable solution.

This is quite different from the case when you have to change/add/delete behavior in a "classic" repository, in which case in order to update one business requirement, you're forced to update a construct that holds a collection of business requirements. That clearly violates SRP and OCP.

### Why using `ThenInclude` in one instance broke the application? What is the proper usage?

In one instance, while describing different usages we updated the specification by adding `ThenInclude` (as shown below), which in turn resulted in a runtime error.

```
Query.Include(x => x.Stores).ThenInclude(x => x.Address);
```

The error here has to do with the fact that the `Address` property is not a navigation property, but a string property. Obviously, you should not include simple properties. And, this is the same behavior that you will have by using EF directly. Including simple properties won't result in a compile-time error but a runtime error. It's up to you to be careful, make proper usage of it, and thoroughly test your queries.

We can constrain this usage and throw an exception, but we don't want to alter the behavior that much. What if the EF in some further version makes use of this usage? So, the ultimate usage constraints should be the responsibility of the ORM you're using.

### How many Include statements are OK to have?

While creating JOINs in SQL, the real issue is not about how many tables, but the cardinality. If the dependent tables are configured as one-to-one relationships, that's quite OK. But if you're including dependent tables, where there are many rows for each principal row, then it can have quite an impact on the performance. On top of that, you should be careful what SQL queries are being produced by EF as well. The EF Core 1&2 uses split queries, while EF Core 3 uses a single query. If you have a lot of 1:n relationships and use a single query, then you might end with a "cartesian explosion" (consider split queries in these cases).

During the [stream](https://www.youtube.com/watch?v=BgWWbBUWyig&t=315s&ab_channel=Ardalis), I showcased the following specification, and the question was if this is OK?

```
public class AwbForInvoiceSpec : Specification<Awb>
{
    public AwbForInvoiceSpec(int ID)
    {
        Query.Where(x => x.ID == ID);
        Query.Include(x => x.Packages);
        Query.Include(x => x.AwbCargoServices);
        Query.Include(x => x.AwbPurchase);
        Query.Include(x => x.CargoManifest);
        Query.Include(x => x.Customer).ThenInclude(x => x.Addresses);
    }
}
```

In the context of that particular application, the `Awb` has quite significant importance in the overall business workflow, and it might be a bit more complicated than it should be. First of all, the `AwbPurchase` and `CargoManifest` represent 1:1 relationships. So, we end up with two 1:n navigations. This is relatively OK if you're retrieving one Awb record (as in this case). On the other hand, if you're trying to get a list of records, then you should reconsider if you need the child collections or not. Try to measure the performance, consider the usage of the application, number of users, peak usages, etc, and then you can decide if that meets your criteria or not.

One key benefit of using the specification pattern is that you can easily have different specifiations that include just the related data necessary for a given operation or context.

### Anti-pattern usage

In the above example, the actual anti-pattern is not the usage of several Include statements, but including the `Customer`. That implies that Awb aggregate has direct navigation to the `Customer` aggregate. If you follow DDD, you should strive to have as independent aggregates as possible. If required, one particular aggregate should hold only the identifier of some other aggregate root and not have a direct reference. The app can then load the other aggregate from its id as necessary.

In our case, it was a deliberate design decision to break this rule (for `Customer` aggregate), in order to improve the performance in particular cases and to reduce roundtrips to DB. But, it's not something I would advise you to do. Anyhow, it's up to you to weigh the pros/cons and make your own elaborate decisions for your applications.

### Do I need private constructors for the entities (e.g. for the EF code-first approach)

I got this question related to one particular scenario which happened during the stream. Once we added the `DateTime birthdate` parameter in the constructor, we were forced to add an additional parameterless constructor so the EF could work properly.

EF requires a parameterless constructor, so it can create the instance of the entity and then populate the properties. So, it might be wise always to add one private parameterless constructor just to be sure EF can instantiate the entity.

EF is smart enough to utilize the constructor and will try to pass the values as ctor arguments. That's how EF handles the immutability (if your props have only getters). But, the ctor arguments should be named the same as the properties. The first character can be lowercase, and that's ok, EF will map to it correctly. But, the case of the rest of the characters should be exactly the same. So, in our case, if we have named the argument `birthDate` instead of `birthdate`, would have worked with no issues.

```
public Customer(string name, string email, string address, DateTime birthdate)
{
    Guard.Against.NullOrEmpty(name, nameof(name));
    Guard.Against.NullOrEmpty(email, nameof(email));

    this.BirthDate = birthdate;
    this.Name = name;
    this.Email = email;
    this.Address = address;
}
```

## Reference

Some free video streams in which this package has been developed and discussed on [YouTube.com/ardalis](http://youtube.com/ardalis?sub_confirmation=1).

- [Reviewing the Specification Pattern and NuGet Package with guest Fiseni](https://www.youtube.com/watch?v=BgWWbBUWyig&t=315s&ab_channel=Ardalis) 6 Nov 2020
- [Open Source .NET Development with Specification and Other Projects](https://www.youtube.com/watch?v=zP_279p2D9w) 14 Jan 2020
- [Updating Specification and GuardClauses Nuget Packages / GitHub Samples](https://www.youtube.com/watch?v=kCeRJj2H1RQ) 20 Nov 2019
- [Ardalis - Working on the Ardalis.Specification and EF Extensions GitHub projects](https://www.youtube.com/watch?v=PbHic9Ndqoc) 16 Aug 2019
- [Working on the Ardalis.Specification Nuget Package and Integration Tests](https://www.youtube.com/watch?v=Ia3zb6-2LuY) 23 July 2019

Pluralsight resources:

- [Design Patterns Library - Specification](https://www.pluralsight.com/courses/patterns-library)
- [Domain-Driven Design Fundamentals](https://www.pluralsight.com/courses/domain-driven-design-fundamentals)
- [Specification Pattern in C#](https://www.pluralsight.com/courses/csharp-specification-pattern)
