namespace FSharp.Data.JsonSchema.Core

open System
open System.Collections.Generic
open System.Reflection
open System.Text.Json.Serialization
open Microsoft.FSharp.Reflection

/// Analyzes F# types and produces SchemaDocument values.
module SchemaAnalyzer =

    let private isOption (ty: Type) =
        ty.IsGenericType
        && let def = ty.GetGenericTypeDefinition()
           in def = typedefof<_ option> || def = typedefof<voption<_>>

    let private isObjOption (ty: Type) =
        ty = typedefof<_ option> || ty = typedefof<voption<_>>

    let private isList (ty: Type) =
        ty.IsGenericType
        && (typedefof<List<_>>.Equals(ty.GetGenericTypeDefinition())
            || typedefof<list<_>>.Equals(ty.GetGenericTypeDefinition()))

    let private isSeq (ty: Type) =
        ty.IsGenericType
        && typedefof<seq<_>>.Equals(ty.GetGenericTypeDefinition())

    let private isResizeArray (ty: Type) =
        ty.IsGenericType
        && typedefof<ResizeArray<_>>.Equals(ty.GetGenericTypeDefinition())

    let private isSet (ty: Type) =
        ty.IsGenericType
        && let def = ty.GetGenericTypeDefinition()
           in def = typedefof<Set<_>>

    let private isArrayLike (ty: Type) =
        ty.IsArray || isList ty || isSeq ty || isResizeArray ty || isSet ty

    let private getArrayElementType (ty: Type) =
        if ty.IsArray then ty.GetElementType()
        elif ty.IsGenericType then ty.GetGenericArguments().[0]
        else failwithf "Not an array-like type: %s" ty.Name

    let private isSkippable (ty: Type) =
        ty.IsGenericType
        && ty.GetGenericTypeDefinition() = typedefof<System.Text.Json.Serialization.Skippable<_>>

    let private isPrimitive (ty: Type) =
        ty.IsPrimitive || ty = typeof<String> || ty = typeof<Decimal>

    let private isIntegerEnum (ty: Type) =
        ty.IsEnum && ty.GetEnumUnderlyingType() = typeof<int>

    let private isDictionary (ty: Type) =
        if ty.IsGenericType then
            let def = ty.GetGenericTypeDefinition()
            def = typedefof<Map<_,_>> || def = typedefof<Dictionary<_,_>>
        else
            false

    let private allCasesEmpty (ty: Type) =
        FSharpType.GetUnionCases(ty)
        |> Array.forall (fun case -> case.GetFields() |> Array.isEmpty)

    let private isChoiceType (ty: Type) =
        if ty.IsGenericType then
            let def = ty.GetGenericTypeDefinition()
            def = typedefof<Choice<_,_>>
            || def = typedefof<Choice<_,_,_>>
            || def = typedefof<Choice<_,_,_,_>>
            || def = typedefof<Choice<_,_,_,_,_>>
            || def = typedefof<Choice<_,_,_,_,_,_>>
            || def = typedefof<Choice<_,_,_,_,_,_,_>>
        else
            false

    let private isAnonymousRecord (ty: Type) =
        // F# compiler generates anonymous records with names like "<>f__AnonymousType..."
        ty.Name.StartsWith("<>f__AnonymousType")

    let private resolveUnionEncoding (config: SchemaGeneratorConfig) (ty: Type) : UnionEncodingStyle =
        // Check for per-type JsonFSharpConverter attribute using CustomAttributeData
        // (properties are write-only, so we must use CustomAttributeData)
        let attrData =
            ty.GetCustomAttributesData()
            |> Seq.tryFind (fun data -> data.AttributeType = typeof<JsonFSharpConverterAttribute>)

        match attrData with
        | None -> config.UnionEncoding
        | Some data ->
            // Find the UnionEncoding named argument
            let unionEncodingArg =
                data.NamedArguments
                |> Seq.tryFind (fun arg -> arg.MemberName = "UnionEncoding")

            match unionEncodingArg with
            | None -> config.UnionEncoding
            | Some arg ->
                // Map FSharp.SystemTextJson JsonUnionEncoding flags to Core UnionEncodingStyle
                let encoding = unbox<JsonUnionEncoding> arg.TypedValue.Value
                if encoding.HasFlag(JsonUnionEncoding.InternalTag) then
                    UnionEncodingStyle.InternalTag
                elif encoding.HasFlag(JsonUnionEncoding.AdjacentTag) then
                    UnionEncodingStyle.AdjacentTag
                elif encoding.HasFlag(JsonUnionEncoding.Untagged) then
                    UnionEncodingStyle.Untagged
                else
                    UnionEncodingStyle.ExternalTag  // Default when no tag flags set (ExternalTag)

    let private primitiveSchema (ty: Type) =
        if ty = typeof<string> then SchemaNode.Primitive(PrimitiveType.String, None)
        elif ty = typeof<int> then SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")
        elif ty = typeof<int64> then SchemaNode.Primitive(PrimitiveType.Integer, Some "int64")
        elif ty = typeof<int16> then SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")
        elif ty = typeof<byte> then SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")
        elif ty = typeof<float> then SchemaNode.Primitive(PrimitiveType.Number, Some "double")
        elif ty = typeof<float32> then SchemaNode.Primitive(PrimitiveType.Number, Some "float")
        elif ty = typeof<decimal> then SchemaNode.Primitive(PrimitiveType.Number, Some "decimal")
        elif ty = typeof<bool> then SchemaNode.Primitive(PrimitiveType.Boolean, None)
        else SchemaNode.Primitive(PrimitiveType.String, None)

    /// Returns true if a type is "simple" and should be inlined rather than referenced.
    let private isSimpleType (config: SchemaGeneratorConfig) (ty: Type) =
        isPrimitive ty
        || isIntegerEnum ty
        || (FSharpType.IsUnion(ty, true) && config.UnwrapFieldlessTags && allCasesEmpty ty)

    /// Analyze an F# type and produce a SchemaDocument.
    let analyze (config: SchemaGeneratorConfig) (targetType: Type) : SchemaDocument =
        let definitions = Dictionary<string, SchemaNode>()
        let visiting = HashSet<Type>()
        let analyzed = Dictionary<Type, string>()

        let getTypeId (ty: Type) = config.TypeIdResolver ty

        /// Get or compute the ref for a complex type. Returns the typeId.
        /// If the type has already been analyzed, returns the existing typeId.
        /// If currently visiting (recursive), returns the typeId for a Ref.
        /// Otherwise, analyzes the type, stores in definitions, returns typeId.
        let rec getOrAnalyzeRef (ty: Type) : string =
            let typeId = getTypeId ty
            if analyzed.ContainsKey ty then
                typeId
            elif visiting.Contains ty then
                // Recursive reference: if it's the root type, use "#" (self-ref)
                if ty = targetType then "#" else typeId
            else
                visiting.Add ty |> ignore
                let schema = analyzeType ty
                definitions.[typeId] <- schema
                analyzed.[ty] <- typeId
                visiting.Remove ty |> ignore
                typeId

        /// Analyze a type to produce a SchemaNode for inline use.
        and analyzeType (ty: Type) : SchemaNode =
            // Handle obj/System.Object
            if ty = typeof<obj> then
                SchemaNode.Any

            // Handle bare generic option (option<_> / voption<_>)
            elif isObjOption ty then
                SchemaNode.Any

            // Handle Skippable<'T> - unwrap to inner type
            elif isSkippable ty then
                let innerTy = ty.GetGenericArguments().[0]
                analyzeType innerTy

            // Handle option<'T> / voption<'T>
            elif isOption ty then
                let innerTy = ty.GetGenericArguments().[0]
                // option<obj> is effectively a bare option<_> — produce empty schema
                if innerTy = typeof<obj> then
                    SchemaNode.Any
                else
                    let innerSchema = analyzeType innerTy
                    SchemaNode.Nullable innerSchema

            // Handle Nullable<'T>
            elif ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<Nullable<_>> then
                let innerTy = ty.GetGenericArguments().[0]
                let innerSchema = analyzeType innerTy
                SchemaNode.Nullable innerSchema

            // Handle DateTime types with format annotations
            elif ty = typeof<DateTime> then
                SchemaNode.Primitive(PrimitiveType.String, Some "date-time")
            elif ty = typeof<DateTimeOffset> then
                SchemaNode.Primitive(PrimitiveType.String, Some "date-time")
            elif ty.FullName = "System.DateOnly" then
                SchemaNode.Primitive(PrimitiveType.String, Some "date")
            elif ty.FullName = "System.TimeOnly" then
                SchemaNode.Primitive(PrimitiveType.String, Some "time")

            // Handle other format-annotated types
            elif ty = typeof<Guid> then
                SchemaNode.Primitive(PrimitiveType.String, Some "guid")
            elif ty = typeof<Uri> then
                SchemaNode.Primitive(PrimitiveType.String, Some "uri")
            elif ty = typeof<TimeSpan> then
                SchemaNode.Primitive(PrimitiveType.String, Some "duration")

            // Handle byte[] as base64-encoded string
            elif ty = typeof<byte[]> then
                SchemaNode.Primitive(PrimitiveType.String, Some "byte")

            // Handle Map<string,'V> and Dictionary<string,'V> as additionalProperties
            elif isDictionary ty then
                let args = ty.GetGenericArguments()
                if args.Length = 2 && args.[0] = typeof<string> then
                    let valueSchema = analyzeFieldSchema args.[1]
                    SchemaNode.Map valueSchema
                else
                    // Non-string keys: fall back to class reflection
                    analyzeClass ty

            // Handle array-like types
            elif isArrayLike ty then
                let elemTy = getArrayElementType ty
                let itemSchema = analyzeFieldSchema elemTy
                SchemaNode.Array itemSchema

            // Handle primitives
            elif isPrimitive ty then
                primitiveSchema ty

            // Handle F# integer-backed enums (shown as string enums in JSON)
            elif isIntegerEnum ty then
                let names = Enum.GetNames(ty) |> Array.toList
                SchemaNode.Enum(names, PrimitiveType.String)

            // Handle Choice types (before general DU check)
            elif isChoiceType ty then
                analyzeChoiceType ty

            // Handle F# DUs
            elif FSharpType.IsUnion(ty, true) then
                analyzeDU ty

            // Handle F# anonymous records (before normal records)
            elif isAnonymousRecord ty then
                analyzeAnonymousRecord ty

            // Handle F# records
            elif FSharpType.IsRecord(ty, true) then
                analyzeRecord ty

            // Handle .NET classes (property-based objects)
            elif ty.IsClass && not ty.IsPrimitive && ty <> typeof<string> then
                analyzeClass ty

            else
                SchemaNode.Any

        /// Returns true if a type should be inlined in field context
        /// (primitives, enums, arrays-of-primitives, Choice types, anonymous records).
        and isInlineType (ty: Type) : bool =
            isSimpleType config ty
            || isArrayLike ty
            || (ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<Nullable<_>>)
            || ty = typeof<obj>
            || isObjOption ty
            || isChoiceType ty
            || isAnonymousRecord ty

        /// Analyze a field type — produces either an inline schema (for primitives)
        /// or a Ref (for complex types that go in definitions).
        and analyzeFieldSchema (ty: Type) : SchemaNode =
            if isSimpleType config ty then
                analyzeType ty
            elif isOption ty then
                let innerTy = ty.GetGenericArguments().[0]
                if isInlineType innerTy then
                    SchemaNode.Nullable (analyzeType innerTy)
                else
                    let typeId = getOrAnalyzeRef innerTy
                    SchemaNode.Nullable (SchemaNode.Ref typeId)
            elif ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<Nullable<_>> then
                let innerTy = ty.GetGenericArguments().[0]
                SchemaNode.Nullable (analyzeType innerTy)
            elif isSkippable ty then
                let innerTy = ty.GetGenericArguments().[0]
                analyzeFieldSchema innerTy
            elif ty = typeof<byte[]> then
                // byte[] is special: treat as base64 string, not array
                SchemaNode.Primitive(PrimitiveType.String, Some "byte")
            elif isDictionary ty then
                // Map/Dictionary needs full analysis via analyzeType
                analyzeType ty
            elif isArrayLike ty then
                let elemTy = getArrayElementType ty
                let itemSchema = analyzeFieldSchema elemTy
                SchemaNode.Array itemSchema
            elif ty = typeof<obj> then
                SchemaNode.Any
            elif isObjOption ty then
                SchemaNode.Any
            elif isChoiceType ty then
                analyzeChoiceType ty
            elif isAnonymousRecord ty then
                analyzeAnonymousRecord ty
            else
                let typeId = getOrAnalyzeRef ty
                SchemaNode.Ref typeId

        /// Analyze a DU case field type — like analyzeFieldSchema but for DU case
        /// context where NJsonSchema creates intermediate definitions for arrays of
        /// complex types.
        and analyzeDuCaseFieldSchema (ty: Type) : SchemaNode =
            // In DU case context, only truly primitive types are inlined.
            // Integer enums, fieldless DUs, classes, records all get $ref.
            if isPrimitive ty then
                analyzeType ty
            elif ty = typeof<byte[]> then
                // byte[] is special: treat as base64 string, not array
                SchemaNode.Primitive(PrimitiveType.String, Some "byte")
            elif isDictionary ty then
                // Map/Dictionary needs full analysis
                analyzeType ty
            elif isArrayLike ty then
                let elemTy = getArrayElementType ty
                if isPrimitive elemTy then
                    analyzeType ty
                else
                    // Array of complex elements: create intermediate definition
                    // NJsonSchema names these as e.g. "TestRecordOf" for TestRecord[]
                    let elemTypeId = getOrAnalyzeRef elemTy
                    let arrayTypeId = elemTypeId + "Of"
                    let arraySchema = SchemaNode.Array(SchemaNode.Ref elemTypeId)
                    definitions.[arrayTypeId] <- arraySchema
                    SchemaNode.Ref arrayTypeId
            else
                let typeId = getOrAnalyzeRef ty
                SchemaNode.Ref typeId

        and analyzeAnonymousRecord (ty: Type) : SchemaNode =
            let fields = FSharpType.GetRecordFields(ty, true)
            let properties = ResizeArray<PropertySchema>()
            let required = ResizeArray<string>()

            for field in fields do
                let propName = config.PropertyNamingPolicy field.Name
                let fieldTy = field.PropertyType

                if isOption fieldTy then
                    let innerTy = fieldTy.GetGenericArguments().[0]
                    if isInlineType innerTy then
                        let innerSchema = analyzeType innerTy
                        properties.Add({ Name = propName; Schema = SchemaNode.Nullable innerSchema; Description = None })
                    else
                        let typeId = getOrAnalyzeRef innerTy
                        properties.Add({ Name = propName; Schema = SchemaNode.Nullable (SchemaNode.Ref typeId); Description = None })
                elif fieldTy.IsGenericType && fieldTy.GetGenericTypeDefinition() = typedefof<Nullable<_>> then
                    let innerTy = fieldTy.GetGenericArguments().[0]
                    let innerSchema = analyzeType innerTy
                    properties.Add({ Name = propName; Schema = SchemaNode.Nullable innerSchema; Description = None })
                elif isSkippable fieldTy then
                    let innerTy = fieldTy.GetGenericArguments().[0]
                    let innerSchema = analyzeFieldSchema innerTy
                    properties.Add({ Name = propName; Schema = innerSchema; Description = None })
                    if config.RecordFieldsRequired then
                        required.Add(propName)
                else
                    let schema = analyzeFieldSchema fieldTy
                    properties.Add({ Name = propName; Schema = schema; Description = None })
                    if config.RecordFieldsRequired then
                        required.Add(propName)

            SchemaNode.Object {
                Properties = Seq.toList properties
                Required = Seq.toList required
                AdditionalProperties = config.AdditionalPropertiesDefault
                TypeId = None
                Description = None
                Title = None
            }

        and analyzeRecord (ty: Type) : SchemaNode =
            let fields = FSharpType.GetRecordFields(ty, true)
            let properties = ResizeArray<PropertySchema>()
            let required = ResizeArray<string>()

            for field in fields do
                let propName = config.PropertyNamingPolicy field.Name
                let fieldTy = field.PropertyType

                if isOption fieldTy then
                    let innerTy = fieldTy.GetGenericArguments().[0]
                    if isInlineType innerTy then
                        let innerSchema = analyzeType innerTy
                        properties.Add({ Name = propName; Schema = SchemaNode.Nullable innerSchema; Description = None })
                    else
                        let typeId = getOrAnalyzeRef innerTy
                        properties.Add({ Name = propName; Schema = SchemaNode.Nullable (SchemaNode.Ref typeId); Description = None })
                elif fieldTy.IsGenericType && fieldTy.GetGenericTypeDefinition() = typedefof<Nullable<_>> then
                    let innerTy = fieldTy.GetGenericArguments().[0]
                    let innerSchema = analyzeType innerTy
                    properties.Add({ Name = propName; Schema = SchemaNode.Nullable innerSchema; Description = None })
                elif isSkippable fieldTy then
                    let innerTy = fieldTy.GetGenericArguments().[0]
                    let innerSchema = analyzeFieldSchema innerTy
                    properties.Add({ Name = propName; Schema = innerSchema; Description = None })
                    if config.RecordFieldsRequired then
                        required.Add(propName)
                else
                    let schema = analyzeFieldSchema fieldTy
                    properties.Add({ Name = propName; Schema = schema; Description = None })
                    if config.RecordFieldsRequired then
                        required.Add(propName)

            SchemaNode.Object {
                Properties = Seq.toList properties
                Required = Seq.toList required
                AdditionalProperties = false
                TypeId = Some (getTypeId ty)
                Description = None
                Title = Some ty.Name
            }

        and analyzeClass (ty: Type) : SchemaNode =
            let props =
                ty.GetProperties(Reflection.BindingFlags.Public ||| Reflection.BindingFlags.Instance)
                |> Array.filter (fun p -> p.CanRead && p.GetIndexParameters().Length = 0)

            let properties = ResizeArray<PropertySchema>()

            for prop in props do
                let propName = config.PropertyNamingPolicy prop.Name
                let schema = analyzeFieldSchema prop.PropertyType
                properties.Add({ Name = propName; Schema = schema; Description = None })

            SchemaNode.Object {
                Properties = Seq.toList properties
                Required = []
                AdditionalProperties = false
                TypeId = Some (getTypeId ty)
                Description = None
                Title = Some ty.Name
            }

        and analyzeChoiceType (ty: Type) : SchemaNode =
            let typeArgs = ty.GetGenericArguments()
            let schemas =
                typeArgs
                |> Array.map analyzeFieldSchema
                |> Array.toList
            SchemaNode.AnyOf schemas

        and analyzeDU (ty: Type) : SchemaNode =
            // Resolve the encoding style (from attribute or config)
            let encodingStyle = resolveUnionEncoding config ty

            // Handle fieldless DUs as string enums
            if config.UnwrapFieldlessTags && allCasesEmpty ty then
                let cases = FSharpType.GetUnionCases(ty, true)
                let names = cases |> Array.map (fun c -> c.Name) |> Array.toList
                SchemaNode.Enum(names, PrimitiveType.String)
            else
                analyzeMultiCaseDU encodingStyle ty

        and buildCaseSchema (encodingStyle: UnionEncodingStyle) (case: Reflection.UnionCaseInfo) : SchemaNode =
            let fields = case.GetFields()

            match encodingStyle with
            | UnionEncodingStyle.InternalTag ->
                // InternalTag: discriminator + fields in same object
                if Array.isEmpty fields then
                    SchemaNode.Const(case.Name, PrimitiveType.String)
                else
                    let properties = ResizeArray<PropertySchema>()
                    let required = ResizeArray<string>()

                    // Add discriminator property
                    let discProp = {
                        Name = config.DiscriminatorPropertyName
                        Schema = SchemaNode.Const(case.Name, PrimitiveType.String)
                        Description = None
                    }
                    properties.Add(discProp)
                    required.Add(config.DiscriminatorPropertyName)

                    // Add case fields
                    for field in fields do
                        let propName = config.PropertyNamingPolicy field.Name
                        let fieldTy = field.PropertyType

                        if isOption fieldTy then
                            let innerTy = fieldTy.GetGenericArguments().[0]
                            if innerTy = typeof<obj> then
                                properties.Add({ Name = propName; Schema = SchemaNode.Any; Description = None })
                            elif isInlineType innerTy then
                                properties.Add({ Name = propName; Schema = analyzeType innerTy; Description = None })
                            else
                                let typeId = getOrAnalyzeRef innerTy
                                properties.Add({ Name = propName; Schema = SchemaNode.Ref typeId; Description = None })
                        elif isPrimitive fieldTy then
                            let schema = analyzeType fieldTy
                            properties.Add({ Name = propName; Schema = schema; Description = None })
                            required.Add(propName)
                        else
                            let schema = analyzeDuCaseFieldSchema fieldTy
                            properties.Add({ Name = propName; Schema = schema; Description = None })
                            required.Add(propName)

                    SchemaNode.Object {
                        Properties = Seq.toList properties
                        Required = Seq.toList required
                        AdditionalProperties = false
                        TypeId = Some case.Name
                        Description = None
                        Title = None
                    }

            | UnionEncodingStyle.AdjacentTag ->
                // AdjacentTag: {"tag": "CaseName", "fields": {...}}
                if Array.isEmpty fields then
                    SchemaNode.Const(case.Name, PrimitiveType.String)
                else
                    let tagProp = {
                        Name = config.DiscriminatorPropertyName
                        Schema = SchemaNode.Const(case.Name, PrimitiveType.String)
                        Description = None
                    }

                    // Build the fields object
                    let fieldProperties = ResizeArray<PropertySchema>()
                    let fieldRequired = ResizeArray<string>()

                    for field in fields do
                        let propName = config.PropertyNamingPolicy field.Name
                        let fieldTy = field.PropertyType

                        if isOption fieldTy then
                            let innerTy = fieldTy.GetGenericArguments().[0]
                            if innerTy = typeof<obj> then
                                fieldProperties.Add({ Name = propName; Schema = SchemaNode.Any; Description = None })
                            elif isInlineType innerTy then
                                fieldProperties.Add({ Name = propName; Schema = analyzeType innerTy; Description = None })
                            else
                                let typeId = getOrAnalyzeRef innerTy
                                fieldProperties.Add({ Name = propName; Schema = SchemaNode.Ref typeId; Description = None })
                        elif isPrimitive fieldTy then
                            let schema = analyzeType fieldTy
                            fieldProperties.Add({ Name = propName; Schema = schema; Description = None })
                            fieldRequired.Add(propName)
                        else
                            let schema = analyzeDuCaseFieldSchema fieldTy
                            fieldProperties.Add({ Name = propName; Schema = schema; Description = None })
                            fieldRequired.Add(propName)

                    let fieldsSchema = SchemaNode.Object {
                        Properties = Seq.toList fieldProperties
                        Required = Seq.toList fieldRequired
                        AdditionalProperties = false
                        TypeId = None
                        Description = None
                        Title = None
                    }

                    let fieldsProp = {
                        Name = "fields"  // Standard property name for AdjacentTag
                        Schema = fieldsSchema
                        Description = None
                    }

                    SchemaNode.Object {
                        Properties = [ tagProp; fieldsProp ]
                        Required = [ config.DiscriminatorPropertyName; "fields" ]
                        AdditionalProperties = false
                        TypeId = Some case.Name
                        Description = None
                        Title = None
                    }

            | UnionEncodingStyle.ExternalTag ->
                // ExternalTag: {"CaseName": {...fields...}}
                if Array.isEmpty fields then
                    // Fieldless case: {"CaseName": {}}
                    let emptyObject = SchemaNode.Object {
                        Properties = []
                        Required = []
                        AdditionalProperties = false
                        TypeId = None
                        Description = None
                        Title = None
                    }
                    let caseProp = {
                        Name = case.Name
                        Schema = emptyObject
                        Description = None
                    }
                    SchemaNode.Object {
                        Properties = [ caseProp ]
                        Required = [ case.Name ]
                        AdditionalProperties = false
                        TypeId = Some case.Name
                        Description = None
                        Title = None
                    }
                else
                    // Build the fields object
                    let fieldProperties = ResizeArray<PropertySchema>()
                    let fieldRequired = ResizeArray<string>()

                    for field in fields do
                        let propName = config.PropertyNamingPolicy field.Name
                        let fieldTy = field.PropertyType

                        if isOption fieldTy then
                            let innerTy = fieldTy.GetGenericArguments().[0]
                            if innerTy = typeof<obj> then
                                fieldProperties.Add({ Name = propName; Schema = SchemaNode.Any; Description = None })
                            elif isInlineType innerTy then
                                fieldProperties.Add({ Name = propName; Schema = analyzeType innerTy; Description = None })
                            else
                                let typeId = getOrAnalyzeRef innerTy
                                fieldProperties.Add({ Name = propName; Schema = SchemaNode.Ref typeId; Description = None })
                        elif isPrimitive fieldTy then
                            let schema = analyzeType fieldTy
                            fieldProperties.Add({ Name = propName; Schema = schema; Description = None })
                            fieldRequired.Add(propName)
                        else
                            let schema = analyzeDuCaseFieldSchema fieldTy
                            fieldProperties.Add({ Name = propName; Schema = schema; Description = None })
                            fieldRequired.Add(propName)

                    let fieldsObject = SchemaNode.Object {
                        Properties = Seq.toList fieldProperties
                        Required = Seq.toList fieldRequired
                        AdditionalProperties = false
                        TypeId = None
                        Description = None
                        Title = None
                    }

                    let caseProp = {
                        Name = case.Name
                        Schema = fieldsObject
                        Description = None
                    }

                    SchemaNode.Object {
                        Properties = [ caseProp ]
                        Required = [ case.Name ]
                        AdditionalProperties = false
                        TypeId = Some case.Name
                        Description = None
                        Title = None
                    }

            | UnionEncodingStyle.Untagged ->
                // Untagged: no discriminator, just fields directly
                if Array.isEmpty fields then
                    // Fieldless case: serialize as case name string
                    SchemaNode.Const(case.Name, PrimitiveType.String)
                else
                    // Build object with just the fields (no discriminator)
                    let properties = ResizeArray<PropertySchema>()
                    let required = ResizeArray<string>()

                    for field in fields do
                        let propName = config.PropertyNamingPolicy field.Name
                        let fieldTy = field.PropertyType

                        if isOption fieldTy then
                            let innerTy = fieldTy.GetGenericArguments().[0]
                            if innerTy = typeof<obj> then
                                properties.Add({ Name = propName; Schema = SchemaNode.Any; Description = None })
                            elif isInlineType innerTy then
                                properties.Add({ Name = propName; Schema = analyzeType innerTy; Description = None })
                            else
                                let typeId = getOrAnalyzeRef innerTy
                                properties.Add({ Name = propName; Schema = SchemaNode.Ref typeId; Description = None })
                        elif isPrimitive fieldTy then
                            let schema = analyzeType fieldTy
                            properties.Add({ Name = propName; Schema = schema; Description = None })
                            required.Add(propName)
                        else
                            let schema = analyzeDuCaseFieldSchema fieldTy
                            properties.Add({ Name = propName; Schema = schema; Description = None })
                            required.Add(propName)

                    SchemaNode.Object {
                        Properties = Seq.toList properties
                        Required = Seq.toList required
                        AdditionalProperties = false
                        TypeId = Some case.Name
                        Description = None
                        Title = None
                    }

        and analyzeMultiCaseDU (encodingStyle: UnionEncodingStyle) (ty: Type) : SchemaNode =
            let cases = FSharpType.GetUnionCases(ty, true)
            let isRoot = (ty = targetType)

            if isRoot then
                // Root DU: register each case in definitions for correct ordering.
                // Referenced types are added by getOrAnalyzeRef during case field analysis,
                // so the order becomes: [referenced type, case using it, ...].
                let caseRefs = ResizeArray<SchemaNode>()
                for case in cases do
                    let caseSchema = buildCaseSchema encodingStyle case
                    definitions.[case.Name] <- caseSchema
                    caseRefs.Add(SchemaNode.Ref case.Name)
                SchemaNode.AnyOf(Seq.toList caseRefs)
            else
                // Non-root DU: inline case schemas in AnyOf (translator nests them)
                let caseSchemas = ResizeArray<SchemaNode>()
                for case in cases do
                    caseSchemas.Add(buildCaseSchema encodingStyle case)
                SchemaNode.AnyOf(Seq.toList caseSchemas)

        // Start analysis
        visiting.Add targetType |> ignore
        let rootSchema = analyzeType targetType
        visiting.Remove targetType |> ignore

        {
            Root = rootSchema
            Definitions = definitions |> Seq.map (fun kv -> kv.Key, kv.Value) |> Seq.toList
        }
