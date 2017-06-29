---
layout: post
title: "Using F# for database related tasks"
description: "Twenty six low-risk ways to use F# at work (part 4)"
categories: []
seriesId: "Low-risk ways to use F# at work"
seriesOrder: 4

---


This post is a continuation of the previous series on [low-risk and incremental ways to use F# at work](/posts/low-risk-ways-to-use-fsharp-at-work/).

In this one, we'll see how F# can be unexpectedly helpful when it comes to database related tasks.

## Series contents

Before moving on to the content of the post, here's the full list of the twenty six ways:

**Part 1 - Using F# to explore and develop interactively**

[1. Use F# to explore the .NET framework interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-net-interactively)<br />
[2. Use F# to test your own code interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-own-code-interactively)<br />
[3. Use F# to play with webservices interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-webservices-interactively)<br />
[4. Use F# to play with UI's interactively](/posts/low-risk-ways-to-use-fsharp-at-work/#explore-winforms-interactively)<br />

**Part 2 - Using F# for development and devops scripts**

[5. Use FAKE for build and CI scripts](/posts/low-risk-ways-to-use-fsharp-at-work-2/#fake)<br />
[6. An F# script to check that a website is responding](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-website-responding)<br />
[7. An F# script to convert an RSS feed into CSV](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-rss-to-csv)<br />
[8. An F# script that uses WMI to check the stats of a process](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-wmi-stats)<br />
[9. Use F# for configuring and managing the cloud](/posts/low-risk-ways-to-use-fsharp-at-work-2/#dev-cloud)<br />

**Part 3 - Using F# for testing**

[10. Use F# to write unit tests with readable names](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-nunit)<br />
[11. Use F# to run unit tests programmatically](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-runner)<br />
[12. Use F# to learn to write unit tests in other ways](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-other)<br />
[13. Use FsCheck to write better unit tests](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-fscheck)<br />
[14. Use FsCheck to create random dummy data](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-dummy)<br />
[15. Use F# to create mocks](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-mock)<br />
[16. Use F# to do automated browser testing](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-canopy)<br />
[17. Use F# for Behaviour Driven Development](/posts/low-risk-ways-to-use-fsharp-at-work-3/#test-bdd)<br />

**Part 4. Using F# for database related tasks**

[18. Use F# to replace LINQpad](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-linqpad)<br />
[19. Use F# to unit test stored procedures](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-testprocs)<br />
[20. Use FsCheck to generate random database records](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-randomdata)<br />
[21. Use F# to do simple ETL](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-etl)<br />
[22. Use F# to generate SQL Agent scripts](/posts/low-risk-ways-to-use-fsharp-at-work-4/#sql-sqlagent)<br />

**Part 5: Other interesting ways of using F# **

[23. Use F# for parsing](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-parsers)<br />
[24. Use F# for diagramming and visualization](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-diagramming)<br />
[25. Use F# for accessing web-based data stores](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-data-access)<br />
[26. Use F# for data science and machine learning](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-data-science)<br />
[(BONUS) 27: Balance the generation schedule for the UK power station fleet](/posts/low-risk-ways-to-use-fsharp-at-work-5/#other-balance-power)<br />

----------

## Part 4. Using F# for database related tasks

This next group of suggestions is all about working with databases, and MS SQL Server in particular.

Relational databases are a critical part of most applications, but most teams do not approach the management of these in the same way as with other development tasks.

For example, how many teams do you know that unit test their stored procedures?

Or their ETL jobs?

Or generate T-SQL admin scripts and other boilerplate using a non-SQL scripting language that's stored in source control?

Here's where F# can shine over other scripting languages, and even over T-SQL itself.

* The database type providers in F# give you the power to create simple, short scripts for testing and admin, with the bonus that...
* The scripts are *type-checked* and will fail at compile time if the database schema changes, which means that...
* The whole process works really well with builds and continuous integration processes, which in turn means that...
* You have really high confidence in your database related code!

We'll look at a few examples to demonstrate what I'm talking about:

* Unit testing stored procedures
* Using FsCheck to generate random records
* Doing simple ETL with F#
* Generating SQL Agent scripts

### Getting set up

The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/SqlInFsharp/).
In there, there are some SQL scripts to create the sample database, tables and stored procs that I'll use in these examples.

To run the examples, then, you'll need SQL Express or SQL Server running locally or somewhere accessible, with the relevant setup scripts having been run.

### Which type provider?

There are a number of SQL Type Providers for F# -- see [the fsharp.org Data Access page](http://fsharp.org/data-access/). For these examples, I'm going to use
the [`SqlDataConnection` type provider](http://msdn.microsoft.com/en-us/library/hh361033.aspx), which is part of the `FSharp.Data.TypeProviders` DLL.
It uses [SqlMetal](http://msdn.microsoft.com/en-us/library/bb386987.aspx) behind the scenes and so only works with SQL Server databases.

The [SQLProvider](http://fsprojects.github.io/SQLProvider/) project is another good choice -- it supports MySql, SQLite and other non-Microsoft databases.

<a name="sql-linqpad"></a>
## 18. Use F# to replace LINQPad

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/SqlInFsharp/FsharpInsteadOfLinqpad.fsx).*

[LINQPad](http://www.linqpad.net/) is a great tool for doing queries against databases, and is also a general scratchpad for C#/VB/F# code.

You can use F# interactive to do many of the same things -- you get queries, autocompletion, etc., just like LINQPad. 

For example, here's one that counts customers with a certain email domain.

```fsharp
[<Literal>]
let connectionString = "Data Source=localhost; Initial Catalog=SqlInFsharp; Integrated Security=True;"

type Sql = SqlDataConnection<connectionString>
let db = Sql.GetDataContext()

// find the number of customers with a gmail domain
query {
    for c in db.Customer do
    where (c.Email.EndsWith("gmail.com"))
    select c
    count
    }
```

If you want to see what SQL code is generated, you can turn logging on, of course:

```fsharp
// optional, turn logging on
db.DataContext.Log <- Console.Out
```

The logged output for this query is:

```text
SELECT COUNT(*) AS [value]
FROM [dbo].[Customer] AS [t0]
WHERE [t0].[Email] LIKE @p0
-- @p0: Input VarChar (Size = 8000; Prec = 0; Scale = 0) [%gmail.com]
```

You can also do more complicated things, such as using subqueries. Here's an example from [MSDN](http://msdn.microsoft.com/en-us/library/hh225374.aspx):

Note that, as befitting a functional approach, queries are nice and composable.

```fsharp
// Find students who have signed up at least one course.
query {
    for student in db.Student do
    where (query { for courseSelection in db.CourseSelection do
                   exists (courseSelection.StudentID = student.StudentID) })
    select student
}
```

And if the SQL engine doesn't support certain functions such as regexes, and assuming the size of the data is not too large,
you can just stream the data out and do the processing in F#.

```fsharp
// find the most popular domain for people born in each decade
let getDomain email =
    Regex.Match(email,".*@(.*)").Groups.[1].Value

let getDecade (birthdate:Nullable<DateTime>) =
    if birthdate.HasValue then
        birthdate.Value.Year / 10  * 10 |> Some
    else
        None

let topDomain list = 
    list
    |> Seq.distinct
    |> Seq.head
    |> snd

db.Customer
|> Seq.map (fun c -> getDecade c.Birthdate, getDomain c.Email)
|> Seq.groupBy fst
|> Seq.sortBy fst
|> Seq.map (fun (decade, group) -> (decade,topDomain group))
|> Seq.iter (printfn "%A")
```

As you can see from the code above, the nice thing about doing the processing in F# is that you can define helper functions separately and connect them together easily.

<a name="sql-testprocs"></a>
## 19. Use F# to unit test stored procedures

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/SqlInFsharp/TestUpsertCustomer.fs).*

Now let's look at how we can use the type provider to make creating unit tests for stored procs really easy.

First, I create a helper module (which I'll call `DbLib`) to set up the connection and to provide shared utility functions such as `resetDatabase`,
which will be called before each test.

```fsharp
module DbLib

[<Literal>]
let connectionString = "Data Source=localhost; Initial Catalog=SqlInFsharp;Integrated Security=True;"
type Sql = SqlDataConnection<connectionString>

let removeExistingData (db:DbContext) = 
    let truncateTable name = 
        sprintf "TRUNCATE TABLE %s" name
        |> db.DataContext.ExecuteCommand 
        |> ignore

    ["Customer"; "CustomerImport"]
    |> List.iter truncateTable

let insertReferenceData (db:DbContext) = 
    [ "US","United States";
      "GB","United Kingdom" ]
    |> List.iter (fun (code,name) -> 
        let c = new Sql.ServiceTypes.Country()
        c.IsoCode <- code;  c.CountryName <- name
        db.Country.InsertOnSubmit c
        )
    db.DataContext.SubmitChanges()

// removes all data and restores db to known starting point
let resetDatabase() =
    use db = Sql.GetDataContext()
    removeExistingData db
    insertReferenceData db
```

Now I can write a unit test, using NUnit say, just like any other unit test.

Assume that we have `Customer` table, and a sproc called `up_Customer_Upsert` that either inserts a new customer or updates an existing one, depending on whether
the passed in customer id is null or not.

Here's what a test looks like:

```fsharp
[<Test>]
let ``When upsert customer called with null id, expect customer created with new id``() = 
    DbLib.resetDatabase() 
    use db = DbLib.Sql.GetDataContext()

    // create customer
    let newId = db.Up_Customer_Upsert(Nullable(),"Alice","x@example.com",Nullable()) 

    // check new id 
    Assert.Greater(newId,0)

    // check one customer exists
    let customerCount = db.Customer |> Seq.length
    Assert.AreEqual(1,customerCount)
```

Note that, because the setup is expensive, I do multiple asserts in the test. This could be refactored if you find this too ugly!

Here's one that tests that updates work:

```fsharp
[<Test>]
let ``When upsert customer called with existing id, expect customer updated``() = 
    DbLib.resetDatabase() 
    use db = DbLib.Sql.GetDataContext()

    // create customer
    let custId = db.Up_Customer_Upsert(Nullable(),"Alice","x@example.com",Nullable()) 
    
    // update customer
    let newId = db.Up_Customer_Upsert(Nullable custId,"Bob","y@example.com",Nullable()) 
    
    // check id hasnt changed
    Assert.AreEqual(custId,newId)

    // check still only one customer
    let customerCount = db.Customer |> Seq.length
    Assert.AreEqual(1,customerCount)

    // check customer columns are updated
    let customer = db.Customer |> Seq.head
    Assert.AreEqual("Bob",customer.Name)
```

And one more, that checks for exceptions:

```fsharp
[<Test>]
let ``When upsert customer called with blank name, expect validation error``() = 
    DbLib.resetDatabase() 
    use db = DbLib.Sql.GetDataContext()

    try
        // try to create customer will a blank name
        db.Up_Customer_Upsert(Nullable(),"","x@example.com",Nullable()) |> ignore
        Assert.Fail("expecting a SqlException")
    with
    | :? System.Data.SqlClient.SqlException as ex ->
        Assert.That(ex.Message,Is.StringContaining("@Name"))
        Assert.That(ex.Message,Is.StringContaining("blank"))
```

As you can see, the whole process is very straightforward. 

These tests can be compiled and run as part of the continuous integration scripts.
And what is great is that, if the database schema gets out of sync with the code, then the tests will fail to even compile!

<a name="sql-randomdata"></a>
## 20. Use FsCheck to generate random database records

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/SqlInFsharp/InsertDummyData.fsx).*

As I showed in an earlier example, you can use FsCheck to generate random data. In this case we'll use it to generate random
records in the database. 

Let's say we have a `CustomerImport` table, defined as below. (We'll use this table in the next section on ETL)

```text
CREATE TABLE dbo.CustomerImport (
	CustomerId int NOT NULL IDENTITY(1,1)
	,FirstName varchar(50) NOT NULL 
	,LastName varchar(50) NOT NULL 
	,EmailAddress varchar(50) NOT NULL 
	,Age int NULL 

	CONSTRAINT PK_CustomerImport PRIMARY KEY CLUSTERED (CustomerId)
	)
```
    
Using the same code as before, we can then generate random instances of `CustomerImport`.    

```fsharp
[<Literal>]
let connectionString = "Data Source=localhost; Initial Catalog=SqlInFsharp; Integrated Security=True;"

type Sql = SqlDataConnection<connectionString>

// a list of names to sample
let possibleFirstNames = 
    ["Merissa";"Kenneth";"Zora";"Oren"]
let possibleLastNames = 
    ["Applewhite";"Feliz";"Abdulla";"Strunk"]

// generate a random name by picking from the list at random
let generateFirstName() = 
    FsCheck.Gen.elements possibleFirstNames 

let generateLastName() = 
    FsCheck.Gen.elements possibleLastNames

// generate a random email address by combining random users and domains
let generateEmail() = 
    let userGen = FsCheck.Gen.elements ["a"; "b"; "c"; "d"; "e"; "f"]
    let domainGen = FsCheck.Gen.elements ["gmail.com"; "example.com"; "outlook.com"]
    let makeEmail u d = sprintf "%s@%s" u d 
    FsCheck.Gen.map2 makeEmail userGen domainGen 
```

So far so good.  

Now we get to the `age` column, which is nullable. This means we can't generate random `int`s, but instead
we have to generate random `Nullable<int>`s. This is where type checking is really useful -- the compiler has forced us to take that into account.
So to make sure we cover all the bases, we'll generate a null value one time out of twenty.
    
```fsharp  
// Generate a random nullable age.
// Note that because age is nullable, 
// the compiler forces us to take that into account
let generateAge() = 
    let nonNullAgeGenerator = 
        FsCheck.Gen.choose(1,99) 
        |> FsCheck.Gen.map (fun age -> Nullable age)
    let nullAgeGenerator = 
        FsCheck.Gen.constant (Nullable())

    // 19 out of 20 times choose a non null age
    FsCheck.Gen.frequency [ 
        (19,nonNullAgeGenerator) 
        (1,nullAgeGenerator)
        ]
```

Putting it altogether...

```fsharp
// a function to create a customer
let createCustomerImport first last email age =
    let c = new Sql.ServiceTypes.CustomerImport()
    c.FirstName <- first
    c.LastName <- last
    c.EmailAddress <- email
    c.Age <- age
    c //return new record

// use applicatives to create a customer generator
let generateCustomerImport = 
    createCustomerImport 
    <!> generateFirstName() 
    <*> generateLastName() 
    <*> generateEmail() 
    <*> generateAge() 
```

Once we have a random generator, we can fetch as many records as we like, and insert them using the type provider.

In the code below, we'll generate 10,000 records, hitting the database in batches of 1,000 records.

```fsharp
let insertAll() =
    use db = Sql.GetDataContext()

    // optional, turn logging on or off
    // db.DataContext.Log <- Console.Out
    // db.DataContext.Log <- null

    let insertOne counter customer =
        db.CustomerImport.InsertOnSubmit customer
        // do in batches of 1000
        if counter % 1000 = 0 then
            db.DataContext.SubmitChanges()

    // generate the records
    let count = 10000
    let generator = FsCheck.Gen.sample 0 count generateCustomerImport

    // insert the records
    generator |> List.iteri insertOne
    db.DataContext.SubmitChanges() // commit any remaining
```

Finally, let's do it and time it.

```fsharp
#time
insertAll() 
#time
```

It's not as fast as using BCP, but it is plenty adequate for testing. For example, it only takes a few seconds to create the 10,000 records above.

I want to stress that this is a *single standalone script*, not a heavy binary, so it is really easy to tweak and run on demand.

And of course you get all the goodness of a scripted approach, such as being able to store it in source control, track changes, etc.

<a name="sql-etl"></a>
## 21. Use F# to do simple ETL

*The code for this section is [available on github](http://github.com/swlaschin/low-risk-ways-to-use-fsharp-at-work/blob/master/SqlInFsharp/EtlExample.fsx).*

Say that you need to transfer data from one table to another, but it is not a totally straightforward copy,
as you need to do some mapping and transformation.

This is a classic ETL (Extract/Transform/Load) situation, and most people will reach for [SSIS](http://en.wikipedia.org/wiki/SQL_Server_Integration_Services).

But for some situations, such as one off imports, and where the volumes are not large, you could use F# instead. Let's have a look.

Say that we are importing data into a master table that looks like this:

```text
CREATE TABLE dbo.Customer (
	CustomerId int NOT NULL IDENTITY(1,1)
	,Name varchar(50) NOT NULL 
	,Email varchar(50) NOT NULL 
	,Birthdate datetime NULL 
	)
```

But the system we're importing from has a different format, like this:

```text
CREATE TABLE dbo.CustomerImport (
	CustomerId int NOT NULL IDENTITY(1,1)
	,FirstName varchar(50) NOT NULL 
	,LastName varchar(50) NOT NULL 
	,EmailAddress varchar(50) NOT NULL 
	,Age int NULL 
	)
```

As part of this import then, we're going to have to:

* Concatenate the `FirstName` and `LastName` columns into one `Name` column
* Map the `EmailAddress` column to the `Email` column
* Calculate a `Birthdate` given an `Age`
* I'm going to skip the `CustomerId` for now -- hopefully we aren't using IDENTITY columns in practice.

The first step is to define a function that maps source records to target records. In this case, we'll call it `makeTargetCustomer`.

Here's some code for this:

```fsharp
[<Literal>]
let sourceConnectionString = 
    "Data Source=localhost; Initial Catalog=SqlInFsharp; Integrated Security=True;"

[<Literal>]
let targetConnectionString = 
    "Data Source=localhost; Initial Catalog=SqlInFsharp; Integrated Security=True;"

type SourceSql = SqlDataConnection<sourceConnectionString>
type TargetSql = SqlDataConnection<targetConnectionString>

let makeName first last = 
    sprintf "%s %s" first last 

let makeBirthdate (age:Nullable<int>) = 
    if age.HasValue then
        Nullable (DateTime.Today.AddYears(-age.Value))
    else
        Nullable()

let makeTargetCustomer (sourceCustomer:SourceSql.ServiceTypes.CustomerImport) = 
    let targetCustomer = new TargetSql.ServiceTypes.Customer()
    targetCustomer.Name <- makeName sourceCustomer.FirstName sourceCustomer.LastName
    targetCustomer.Email <- sourceCustomer.EmailAddress
    targetCustomer.Birthdate <- makeBirthdate sourceCustomer.Age
    targetCustomer // return it
```

With this transform in place, the rest of the code is easy, we just just read from the source and write to the target.

```fsharp
let transferAll() =
    use sourceDb = SourceSql.GetDataContext()
    use targetDb = TargetSql.GetDataContext()

    let insertOne counter customer =
        targetDb.Customer.InsertOnSubmit customer
        // do in batches of 1000
        if counter % 1000 = 0 then
            targetDb.DataContext.SubmitChanges()
            printfn "...%i records transferred" counter 

    // get the sequence of source records
    sourceDb.CustomerImport
    // transform to a target record
    |>  Seq.map makeTargetCustomer 
    // and insert
    |>  Seq.iteri insertOne
    
    targetDb.DataContext.SubmitChanges() // commit any remaining
    printfn "Done"
```

Because these are sequence operations, only one record at a time is in memory (excepting the LINQ submit buffer), so even large data sets can
be processed.

To see it in use, first insert a number of records using the dummy data script just discussed, and then run the transfer as follows:

```fsharp
#time
transferAll() 
#time
```

Again, it only takes a few seconds to transfer 10,000 records.

And again, this is a *single standalone script* -- it's a very lightweight way to create simple ETL jobs.

<a name="sql-sqlagent"></a>
## 22. Use F# to generate SQL Agent scripts 

For the last database related suggestion, let me suggest the idea of generating SQL Agent scripts from code.

In any decent sized shop you may have hundreds or thousands of SQL Agent jobs.  In my opinion, these should all be stored as script files, and 
loaded into the database when provisioning/building the system.

Alas, there are often subtle differences between dev, test and production environments: connection strings, authorization, alerts, log configuration, etc.

That naturally leads to the problem of trying to keep three different copies of a script around, which in turn makes you think:
why not have *one* script and parameterize it for the environment?

But now you are dealing with lots of ugly SQL code! The scripts that create SQL agent jobs are typically hundreds of lines long and were not really designed
to be maintained by hand.

F# to the rescue!

In F#, it's really easy to create some simple record types that store all the data you need to generate and configure a job.

For example, in the script below:

* I created a union type called `Step` that could store a `Package`, `Executable`, `Powershell` and so on.
* Each of these step types in turn have their own specific properties, so that a `Package` has a name and variables, and so on.
* A `JobInfo` consists of a name plus a list of `Step`s.
* An agent script is generated from a `JobInfo` plus a set of global properties associated with an environment, such as the databases, shared folder locations, etc.

```fsharp
let thisDir = __SOURCE_DIRECTORY__
System.IO.Directory.SetCurrentDirectory (thisDir)

#load @"..\..\SqlAgentLibrary.Lib.fsx"
      
module MySqlAgentJob = 

    open SqlAgentLibrary.Lib.SqlAgentLibrary
    
    let PackageFolder = @"\shared\etl\MyJob"

    let step1 = Package {
        Name = "An SSIS package"
        Package = "AnSsisPackage.dtsx"
        Variables = 
            [
            "EtlServer", "EtlServer"
            "EtlDatabase", "EtlDatabase"
            "SsisLogServer", "SsisLogServer"
            "SsisLogDatabase", "SsisLogDatabase"
            ]
        }

    let step2 = Package {
        Name = "Another SSIS package"
        Package = "AnotherSsisPackage.dtsx"
        Variables = 
            [
            "EtlServer", "EtlServer2"
            "EtlDatabase", "EtlDatabase2"
            "SsisLogServer", "SsisLogServer2"
            "SsisLogDatabase", "SsisLogDatabase2"
            ]
        }

    let jobInfo = {
        JobName = "My SqlAgent Job"
        JobDescription = "Copy data from one place to another"
        JobCategory = "ETL"
        Steps = 
            [
            step1
            step2
            ]
        StepsThatContinueOnFailure = []
        JobSchedule = None
        JobAlert = None
        JobNotification = None
        }            
        
    let generate globals = 
        writeAgentScript globals jobInfo 
        
module DevEnvironment = 

    let globals = 
        [
        // global
        "Environment", "DEV"
        "PackageFolder", @"\shared\etl\MyJob"
        "JobServer", "(local)"

        // General variables
        "JobName", "Some packages"
        "SetStartFlag", "2"
        "SetEndFlag", "0"

        // databases
        "Database", "mydatabase"
        "Server",  "localhost"
        "EtlServer", "localhost"
        "EtlDatabase", "etl_config"

        "SsisLogServer", "localhost"
        "SsisLogDatabase", "etl_config"
        ] |> Map.ofList


    let generateJob() = 
        MySqlAgentJob.generate globals    

DevEnvironment.generateJob()
```

I can't share the actual F# code, but I think you get the idea. It's quite simple to create.

Once we have these .FSX files, we can generate the real SQL Agent scripts en-masse and then deploy them to the appropriate servers.

Below is an example of a SQL Agent script that might be generated automatically from the .FSX file. 

As you can see, it is a nicely laid out and formatted T-SQL script. The idea is that a DBA can review it and be confident that no magic is happening, and thus be
willing to accept it as input.  

On the other hand, it would be risky to maintain scripts like. Editing the SQL code directly could be risky.
Better to use type-checked (and more concise) F# code than untyped T-SQL!

```sql
USE [msdb]
GO

-- =====================================================
-- Script that deletes and recreates the SQL Agent job 'My SqlAgent Job'
-- 
-- The job steps are:
-- 1) An SSIS package
     -- {Continue on error=false} 
-- 2) Another SSIS package
     -- {Continue on error=false} 

-- =====================================================


-- =====================================================
-- Environment is DEV
-- 
-- The other global variables are:
-- Database = mydatabase
-- EtlDatabase = etl_config
-- EtlServer = localhost
-- JobName = My SqlAgent Job
-- JobServer = (local)
-- PackageFolder = \\shared\etl\MyJob\
-- Server = localhost
-- SetEndFlag = 0
-- SetStartFlag = 2
-- SsisLogDatabase = etl_config
-- SsisLogServer = localhost

-- =====================================================


-- =====================================================
-- Create job
-- =====================================================

-- ---------------------------------------------
-- Delete Job if it exists
-- ---------------------------------------------
IF  EXISTS (SELECT job_id FROM msdb.dbo.sysjobs_view WHERE name = 'My SqlAgent Job') 
BEGIN
    PRINT 'Deleting job "My SqlAgent Job"'
    EXEC msdb.dbo.sp_delete_job @job_name='My SqlAgent Job', @delete_unused_schedule=0
END	

-- ---------------------------------------------
-- Create Job
-- ---------------------------------------------

BEGIN TRANSACTION
DECLARE @ReturnCode INT
SELECT @ReturnCode = 0

-- ---------------------------------------------
-- Create Category if needed
-- ---------------------------------------------
IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name='ETL' AND category_class=1)
BEGIN
    PRINT 'Creating category "ETL"'
    EXEC @ReturnCode = msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name='ETL'
    IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
END

-- ---------------------------------------------
-- Create Job 
-- ---------------------------------------------

DECLARE @jobId BINARY(16)
PRINT 'Creating job "My SqlAgent Job"'
EXEC @ReturnCode =  msdb.dbo.sp_add_job @job_name='My SqlAgent Job', 
        @enabled=1, 
        @category_name='ETL', 
        @owner_login_name=N'sa', 
        @description='Copy data from one place to another',
        @job_id = @jobId OUTPUT

IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback


PRINT '-- ---------------------------------------------'
PRINT 'Create step 1: "An SSIS package"'
PRINT '-- ---------------------------------------------'
DECLARE @Step1_Name nvarchar(50) = 'An SSIS package'
DECLARE @Step1_Package nvarchar(170) = 'AnSsisPackage.dtsx'
DECLARE @Step1_Command nvarchar(1700) = 
    '/FILE "\\shared\etl\MyJob\AnSsisPackage.dtsx"' + 
    ' /CHECKPOINTING OFF' + 
    ' /SET "\Package.Variables[User::SetFlag].Value";"2"' + 
    ' /SET "\Package.Variables[User::JobName].Value";""' + 
    ' /SET "\Package.Variables[User::SourceServer].Value";"localhost"' + 
    ' /SET "\Package.Variables[User::SourceDatabaseName].Value";"etl_config"' + 

    ' /REPORTING E'

EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=@Step1_Name, 
        @step_id=1, 
        @on_success_action=3, 
        @on_fail_action=2,
        @subsystem=N'SSIS', 
        @command=@Step1_Command
          
        IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback


PRINT '-- ---------------------------------------------'
PRINT 'Create step 2: "Another SSIS Package"'
PRINT '-- ---------------------------------------------'
DECLARE @Step2_Name nvarchar(50) = 'Another SSIS Package'
DECLARE @Step2_Package nvarchar(170) = 'AnotherSsisPackage.dtsx'
DECLARE @Step2_Command nvarchar(1700) = 
    '/FILE "\\shared\etl\MyJob\AnotherSsisPackage.dtsx.dtsx"' + 
    ' /CHECKPOINTING OFF' + 
    ' /SET "\Package.Variables[User::EtlServer].Value";"localhost"' + 
    ' /SET "\Package.Variables[User::EtlDatabase].Value";"etl_config"' + 
    ' /SET "\Package.Variables[User::SsisLogServer].Value";"localhost"' + 
    ' /SET "\Package.Variables[User::SsisLogDatabase].Value";"etl_config"' + 

    ' /REPORTING E'

EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=@Step2_Name, 
        @step_id=2, 
        @on_success_action=3, 
        @on_fail_action=2,
        @subsystem=N'SSIS', 
        @command=@Step2_Command
          
        IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

    -- ---------------------------------------------
-- Job Schedule
-- ---------------------------------------------


-- ----------------------------------------------
-- Job Alert
-- ----------------------------------------------


-- ---------------------------------------------
-- Set start step
-- ---------------------------------------------

EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @jobId, @start_step_id = 1
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

-- ---------------------------------------------
-- Set server
-- ---------------------------------------------


EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = '(local)'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback


PRINT 'Done!'

COMMIT TRANSACTION
GOTO EndSave
QuitWithRollback:
    IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION
EndSave:
GO
```


 
## Summary

I hope that this set of suggestions has thrown a new light on what F# can be used for.

In my opinion, the combination of concise syntax, lightweight scripting (no binaries) and SQL type providers makes
F# incredibly useful for database related tasks. 

Please leave a comment and let me know what you think. 





