## Prerequisites to Extending your Code 
Each Project gets turned into a DLL file (Dynamic-Link Library)

> [!Note]
> **DLL/Assembly Composition**
> 1. Manifest: Assembly level info (its descripe itself and the things it depends on)
> 2. Metadata: Structured Info on types, methods, fields, refs and co...
> 3. Compiled platform independent code
> 4. Resources: Optional resources (images, strings etc.)

The Metadata is like a middleman API, it allows us to search and get types, without the need to execute the code.

Assemblies are identified by their 
1. Name (ex. Serilog)
2. Version (ex. 1.0.0.0)
3. Culture (ex. neutral, the language used for the info/ui/... (de, fr, neutral/en))
4. PublicKeyToken (ex. 1a2b... Optional, Identifies publisher)

> [!Tip]
> **Culture Use case**\
> Using: `    <EmbeddedResource Include="Resources\Strings.resx" />`.\
> The compiler compiles `.lang.resx` which are *xml* files into their appropriate dll file.\
> In Runtime you cen then access different languages by loading those in.

> [!Warning]
> Different DLL's libaries with the same **Version** can create conflicts and problems.\
> Each change should have its own **Version**.

When a built is compiled, the .deps.json is the map to all its dependencies

Continue at Lesson 2.

```csproj
<Project Sdk="Microsoft.NET.Sdk"
...
```