---
layout: post
title: "Comparing F# with C#: Downloading a web page"
description: "In which we see that F# excels at callbacks, and we are introduced to the 'use' keyword"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 5
categories: [F# vs C#]
---

In this example, we will compare the F# and C# code for downloading a web page, with a callback to process the text stream.

We'll start with a straightforward F# implementation. 

```fsharp
// "open" brings a .NET namespace into visibility
open System.Net
open System
open System.IO

// Fetch the contents of a web page
let fetchUrl callback url =        
    let req = WebRequest.Create(Uri(url)) 
    use resp = req.GetResponse() 
    use stream = resp.GetResponseStream() 
    use reader = new IO.StreamReader(stream) 
    callback reader url
```

Let's go through this code:

* The use of "open" at the top allows us to write "WebRequest" rather than "System.Net.WebRequest". It is similar to a "`using System.Net`" header in C#.
* Next, we define the `fetchUrl` function, which takes two arguments, a callback to process the stream, and the url to fetch. 
* We next wrap the url string in a Uri. F# has strict type-checking, so if instead we had written:
`let req = WebRequest.Create(url)`
the compiler would have complained that it didn't know which version of `WebRequest.Create` to use.
* When declaring the `response`, `stream` and `reader` values, the "`use`" keyword is used instead of "`let`". This can only be used in conjunction with classes that implement `IDisposable`.
  It tells the compiler to automatically dispose of the resource when it goes out of scope. This is equivalent to the C# "`using`" keyword.
* The last line calls the callback function with the StreamReader and url as parameters.  Note that the type of the callback does not have to be specified anywhere.

Now here is the equivalent C# implementation. 

```csharp
class WebPageDownloader
{
    public TResult FetchUrl<TResult>(
        string url,
        Func<string, StreamReader, TResult> callback)
    {
        var req = WebRequest.Create(url);
        using (var resp = req.GetResponse())
        {
            using (var stream = resp.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    return callback(url, reader);
                }
            }
        }
    }
}
```

As usual, the C# version has more 'noise'. 

* There are ten lines just for curly braces, and there is the visual complexity of 5 levels of nesting*
* All the parameter types have to be explicitly declared, and the generic `TResult` type has to be repeated three times.

<sub>* It's true that in this particular example, when all the `using` statements are adjacent, the [extra braces and indenting can be removed](https://stackoverflow.com/questions/1329739/nested-using-statements-in-c-sharp),
but in the more general case they are needed.</sub>

## Testing the code

Back in F# land, we can now test the code interactively:

```fsharp
let myCallback (reader:IO.StreamReader) url = 
    let html = reader.ReadToEnd()
    let html1000 = html.Substring(0,1000)
    printfn "Downloaded %s. First 1000 is %s" url html1000
    html      // return all the html

//test
let google = fetchUrl myCallback "http://google.com"
```

Finally, we have to resort to a type declaration for the reader parameter (`reader:IO.StreamReader`). This is required because the F# compiler cannot determine the type of the "reader" parameter automatically. 

A very useful feature of F# is that you can "bake in" parameters in a function so that they don't have to be passed in every time. This is why the `url` parameter was placed *last* rather than first, as in the C# version.
The callback can be setup once, while the url varies from call to call.

```fsharp
// build a function with the callback "baked in"
let fetchUrl2 = fetchUrl myCallback 

// test
let google = fetchUrl2 "http://www.google.com"
let bbc    = fetchUrl2 "http://news.bbc.co.uk"

// test with a list of sites
let sites = ["http://www.bing.com";
             "http://www.google.com";
             "http://www.yahoo.com"]

// process each site in the list
sites |> List.map fetchUrl2 
```

The last line (using `List.map`) shows how the new function can be easily used in conjunction with list processing functions to download a whole list at once. 

Here is the equivalent C# test code:

```csharp
[Test]
public void TestFetchUrlWithCallback()
{
    Func<string, StreamReader, string> myCallback = (url, reader) =>
    {
        var html = reader.ReadToEnd();
        var html1000 = html.Substring(0, 1000);
        Console.WriteLine(
            "Downloaded {0}. First 1000 is {1}", url,
            html1000);
        return html;
    };

    var downloader = new WebPageDownloader();
    var google = downloader.FetchUrl("http://www.google.com",
                                      myCallback);
            
    // test with a list of sites
    var sites = new List<string> {
        "http://www.bing.com",
        "http://www.google.com",
        "http://www.yahoo.com"};

    // process each site in the list
    sites.ForEach(site => downloader.FetchUrl(site, myCallback));
}
```

Again, the code is a bit noisier than the F# code, with many explicit type references. More importantly, the C# code doesn't easily allow you to bake in some of the parameters in a function, so the callback must be explicitly referenced every time.
