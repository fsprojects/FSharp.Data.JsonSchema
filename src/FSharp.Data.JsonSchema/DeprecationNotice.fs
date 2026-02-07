namespace FSharp.Data.JsonSchema

open System

/// <summary>
/// DEPRECATED: The FSharp.Data.JsonSchema package has been renamed to FSharp.Data.JsonSchema.NJsonSchema.
/// </summary>
/// <remarks>
/// This package is a compatibility shim that references FSharp.Data.JsonSchema.NJsonSchema.
/// Please update your package references to use FSharp.Data.JsonSchema.NJsonSchema directly.
/// This package will not receive updates beyond version 3.0.0.
/// </remarks>
[<Obsolete("This package has been renamed to FSharp.Data.JsonSchema.NJsonSchema. Please update your package reference to FSharp.Data.JsonSchema.NJsonSchema.", false)>]
module DeprecationNotice =

    /// <summary>
    /// This package has been renamed. Use FSharp.Data.JsonSchema.NJsonSchema instead.
    /// </summary>
    [<Literal>]
    let Message = "FSharp.Data.JsonSchema has been renamed to FSharp.Data.JsonSchema.NJsonSchema. Please update your package reference."

    /// <summary>
    /// The new package name to use.
    /// </summary>
    [<Literal>]
    let NewPackageName = "FSharp.Data.JsonSchema.NJsonSchema"
