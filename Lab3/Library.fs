namespace MyCompiler

open System
open System.Reflection
open System.Reflection.Emit
open FParsec
open Microsoft.FSharp.Collections

module Compiler =    
    type BinaryExprKind = 
        | Add
        | Sub
        | Multiply
        | Divide
    
    type Expr = 
        | IntLiteral of int
        | FloatLiteral of float
        | StringLiteral of string
        | Identifier of string
        | Binary of (Expr * Expr * BinaryExprKind)
    
    let quote : Parser<_, unit> = skipChar '\"'
    let stringLiteral = quote  >>. many1CharsTill anyChar quote |>> Expr.StringLiteral
    
    let intOrFloatLiteral : Parser<_, unit> = 
        numberLiteral (NumberLiteralOptions.DefaultFloat ||| NumberLiteralOptions.DefaultInteger) "number" 
        |>> fun n -> 
                if n.IsInteger then  Expr.IntLiteral (int n.String)
                else Expr.FloatLiteral (float n.String)
    
    let identifier = many1Chars (letter <|> digit) |>> Expr.Identifier
    
    let opp = OperatorPrecedenceParser<Expr, _, _>()
    
    let operatorsSeq = [
        intOrFloatLiteral
        stringLiteral
        identifier
    ]
    
    opp.TermParser <- choice (operatorsSeq 
                              |> Seq.map (fun op -> between spaces spaces op) 
                              |> Seq.append (Seq.singleton (between (skipChar '(' .>> spaces ) (skipChar ')' .>> spaces) opp.ExpressionParser)))
    
    opp.AddOperator <| InfixOperator("*", spaces, 4, Associativity.Left, fun x y -> Expr.Binary(x, y, BinaryExprKind.Multiply))
    opp.AddOperator <| InfixOperator("/", spaces, 3, Associativity.Left, fun x y -> Expr.Binary(x, y, BinaryExprKind.Divide))
    opp.AddOperator <| InfixOperator("-", spaces, 2, Associativity.Left, fun x y -> Expr.Binary(x, y, BinaryExprKind.Sub))
    opp.AddOperator <| InfixOperator("+", spaces, 1, Associativity.Left, fun x y -> Expr.Binary(x, y, BinaryExprKind.Add))
    
    
    let parseSignature = between spaces spaces (skipString "function " >>. spaces .>> identifier >>. between (skipChar '(' .>> spaces) (spaces >>. skipChar ')') (sepEndBy (many1Chars (letter <|> digit)) (skipChar ',' .>> spaces)))
    
    let parseBody = between (skipChar '{' .>> spaces) (between spaces spaces (skipChar '}')) (opp.ExpressionParser .>> spaces .>> skipChar ';')
    
    let parseFn = parseSignature .>>. parseBody

    let rec emitExpr (il: ILGenerator) args expr = 
        match expr with
        | IntLiteral i -> 
            il.Emit(OpCodes.Ldc_I4, i)
            il.Emit(OpCodes.Conv_R8)
        | FloatLiteral f -> 
            il.Emit(OpCodes.Ldc_R8, f)
        | Identifier ident -> 
            let index = args |> List.findIndex(fun e -> e.Equals(ident))
            il.Emit(OpCodes.Ldarg, index)
        | Binary (l, r, exprKind) ->
            emitExpr il args l
            emitExpr il args r
            match exprKind with
                | Add -> il.Emit(OpCodes.Add)
                | Sub -> il.Emit(OpCodes.Sub)
                | Multiply -> il.Emit(OpCodes.Mul)
                | Divide -> il.Emit(OpCodes.Div)
        | _ -> failwith "todo"
    
    
    let generateMethod (mb: MethodBuilder) code = 
        match run (parseFn .>> eof) code with
        | Success(result, _, _)   -> 
            let args, expr = result
            let argsArray = [| for _ in 1..args.Length -> typeof<double> |]
    
            mb.SetParameters(argsArray)
            mb.SetReturnType(typeof<double>)

            let il = mb.GetILGenerator()
            emitExpr il args expr
            il.Emit(OpCodes.Ret)

        | Failure(errorMsg, _, _) -> 
            raise(Exception(errorMsg))
    
    let compile code =
        let asmName = AssemblyName("temp")
        let mutable asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect)
        let mutable demoModule = asmBuilder.DefineDynamicModule(asmName.Name + ".dll")
        let mutable demoType = demoModule.DefineType("Program")
        let mutable mb = demoType.DefineMethod("Method", MethodAttributes.Public ||| MethodAttributes.Static)
       
        generateMethod mb code
    
        let ty = demoType.CreateType()
        
        ty.GetMethod("Method")
    
    let run (method: MethodInfo) (args: double[]) = 
        let args = args |> Array.map (box)
        method.Invoke(null, args)
       
