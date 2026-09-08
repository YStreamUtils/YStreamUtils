# YStreamUtils

## CURRENTLY VERY WIP, I HAVE MOST OF IT WORKING I THINK?

## The Stream Companion You Didn't Ask For

I keep bouncing around between C# + C#/js/py and go + wasm, but I have decided to just use go + js/ts.

I only use clankers for research, I know my implementation is bad... make a PR idk.

Many of the concepts for caching and compiling I took from [My Other Project](https://github.com/ysnt-yes/YScriptEngine).

Nobody wants to script with C#, it's literally just TS. Have you seen it?

```csharp
// CSharp
var logger = GetService<ILogger>();
// or
var logger = GetService("Logger");
```

```js
// JS
const logger = GetService("Logger")
const logger = host.log("info", "blah blah blah")
// or whatever idk
```

### [Currently Supported Events](https://github.com/YStreamUtils/YStreamUtils/blob/master/YStreamUtils/Core/Models/EventKey.cs)

### [Current Roadmap](TODO.md)

### [Plugin Repo](https://github.com/YStreamUtils/YStreamUtils-Plugin-Registry)

For those who care... THE STACK!

- Dotnet 10
- ASP.Net for the apis
- Jint
- Docker and Desktop compatible, same binary
- PWA available (evnetually)
- Svelte 5 (not kit, just a SPA I guess?)

This project was initially in Go but I moved it over to ASP.Net + dotnet 10.
The decision primarily centered around DI, tooling, and the fact that Jint supports ES6+ while Goja only does ES5 plus some other features.

<!-- <style>h1,h2,h3,h4 { border-bottom: 0; } </style> -->
