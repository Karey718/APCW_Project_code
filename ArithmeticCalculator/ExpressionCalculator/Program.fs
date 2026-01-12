// Simple Interpreter in F#
// Author: C9 Boshuo He(100525654)
// Date: 23/10/2022
// Reference: Peter Sestoft, Grammars and parsing with F#, Tech. Report

namespace ExpressionCalculator


module public Calculator =
    open System


    let lexError = System.Exception("Lexer error")
    let parseError = System.Exception("Parser error")
    let divError = System.Exception("Division error( div zero )")
    let typeError = System.Exception("Type Error")
    let nameError = System.Exception("Name Error")
    let logError = System.Exception("Log Argument Error")

    


    type Value = 
        | I of int
        | F of float

    

    type TypeAnn = TInt | TFloat

    type VarSym = {
        DeclaredType : TypeAnn option   // None - Var not declared
        Value        : Value option     // None - Value not initialized
    }

    type Environment = Map<string,VarSym>
    let emptyEnv : Environment = Map.empty

    let GetEmptyEnv() = Map.empty<string, VarSym>

    let valueType = function | I _ -> TInt | F _ -> TFloat

    let checkToInt (v: Value) =
        match v with
        | I _ -> v
        | F f ->
            let r = Math.Round f
            if abs(f - r) < 1e-12 then I (int r) else raise typeError

    let checkToFloat (v: Value) =
        match v with
        | F _ -> v
        | I i -> F (float i)

    let fitToDeclared (decl: TypeAnn option) (v: Value) =
        match decl with
        | None -> v
        | Some TInt -> checkToInt v
        | Some TFloat -> checkToFloat v

    let setSym (name:string) (decl: TypeAnn option) (valueOpt: Value option) (env:Environment) =
        env.Add(name, { DeclaredType = decl; Value = valueOpt })

    let getSym (name:string) (env:Environment) =
        match env.TryFind name with
        | None      -> raise nameError
        | Some sym  -> sym

    let valueOfId (name:string) (env:Environment) =
        let s = getSym name env
        match s.Value with
        | None      -> raise nameError       
        | Some v    -> v

    let updateSymValue (name:string) (v:Value) (env:Environment) =
        match env.TryFind name with
        | None ->
            setSym name None (Some v) env
        | Some s ->
            let vv = fitToDeclared s.DeclaredType v
            env.Add(name, { s with Value = Some vv })

    let declareSym (name:string) (decl:TypeAnn) (init: Value option) (env:Environment) =
        let init' = init |> Option.map (fitToDeclared (Some decl))
        setSym name (Some decl) init' env

    let assignId (name:string) (v:Value) (env:Environment) : Environment =
        updateSymValue name v env

    let declareInt  name init env = declareSym name TInt   init env
    let declareFloat name init env = declareSym name TFloat init env


    module Value = 
        let asFloat = function
            | I i -> float i
            | F f -> f

        let asIntExact = function
            | I i -> Some i
            | F f when abs ( f - Math.Round(f) ) < 1e-12 -> Some (int ( Math.Round(f) ))
            | _ -> None

        let add a b =
            match a , b  with
            | I x , I y -> I ( x + y )
            | _ -> F (asFloat a + asFloat b )
        let sub a b =
            match a, b with
            | I x, I y -> I (x - y)
            | _ -> F (asFloat a - asFloat b)

        let mul a b =
            match a, b with
            | I x, I y -> I (x * y)
            | _ -> F (asFloat a * asFloat b)

        let div a b =
            match a, b with
            | _, (I 0) -> raise divError
            | _, (F fb) when abs (fb) < 1e-16 -> raise divError
            | I x , I y ->
                if y = 0 then raise divError
                else I (x / y)

            | _ ->
                let denom = asFloat b
                if abs denom < 1e-16 then raise divError
                F (asFloat a / denom)

        let rem a b =
            match a, b with
            | _, I 0 -> raise divError
            | _, F fb when abs fb < 1e-16 -> raise divError
            | I x, I y -> I (x % y)
            | _ ->
                let af, bf = asFloat a, asFloat b
                if abs bf < 1e-16 then raise divError
                let q = Math.Truncate(af / bf)
                F (af - q * bf)

        let pow a b =
            match a, b with
            | I 0, I 0 -> raise parseError   
            | I x, I y when y >= 0 ->
                let rec ipow b exp acc =
                    if exp = 0 then acc
                    elif (exp &&& 1) = 1 then ipow b (exp - 1) (acc * b)
                    else ipow (b * b) (exp >>> 1) acc
                I (ipow x y 1)
            | _ ->
                let r = Math.Pow(asFloat a, asFloat b)
                match asIntExact (F r) with
                | Some i -> I i
                | None -> F r

        let negate = function
            | I x -> I (-x)
            | F x -> F (-x)

        let private ofFloatSmart (f: float) =
            if Double.IsNaN f || Double.IsInfinity f then
                F f
            else 
                let r = Math.Round f
                if abs(f-r) < 1e-12 && r <= float Int32.MaxValue && r >= float Int32.MinValue then
                    I( int r)
                else
                    F f


        let sinv v = ofFloatSmart ( Math.Sin(asFloat v) )
        let cosv v = ofFloatSmart ( Math.Cos(asFloat v) )
        let tanv v = 
            let x = asFloat v
            let c = Math.Cos x
            if abs c < 1e-12 then
                let s = Math.Sin x
                let sign = if s >= 0.0 then 1.0 else -1.0
                F (sign * Double.PositiveInfinity)
            else
                ofFloatSmart ( Math.Tan(asFloat v) )

        let expv v = ofFloatSmart ( Math.Exp(asFloat v) )
        let lnv v =
            let x = asFloat v
            if x <= 0.0 then raise logError
            else ofFloatSmart ( Math.Log x )
        let log10v v =
            let x = asFloat v
            if x <= 0.0 then raise logError
            else ofFloatSmart ( Math.Log10 x )
    


    type terminal = 
        Add | Sub | Mul | Div | Rem | Pow | Lpar | Rpar | Dot | Num of Value
        | NaturalLogE 
        | Sin | Cos | Tan | Pi
        | Exp | Log | Ln
        | Id of string
        | Assign // '='
        | Semi // ';'
        | VarInt // "int"
        | VarFloat // "Float"

    let str2lst s = [for c in s -> c]
    let isblank c = System.Char.IsWhiteSpace c
    let isdigit c = System.Char.IsDigit c
    let isalpha c = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
    let isnumalpha c = isdigit c || isalpha c
    let isidstart c = isalpha c || c = '_'
    let isidtail c = isnumalpha c || c = '_'

    let intVal (c:char) = (int)((int)c - (int)'0')

    

    let rec scInt(iStr, iVal) = 
        match iStr with
        c :: tail when isdigit c -> scInt(tail, 10*iVal+(intVal c))
        | _ -> (iStr, iVal)

    let rec scFloat(fStr, iPart, dPart, dPlaces) =
        match fStr with
        | c :: tail when isdigit c ->
            scFloat(tail, iPart, 10.0*dPart + float(intVal c), dPlaces+1.0)
        | _ ->
            let fVal = iPart + dPart / (10.0 ** dPlaces)
            (fStr, fVal)

    let rec scanId cs acc = 
        match cs with
        | c :: tail when isidtail c -> scanId tail ( acc + string c )
        | _ -> (cs , acc)
(*
    let scanNumber (chars: char list) : (char list * terminal) =
        match chars with
        | '.' :: tail ->
            match tail with
            | c1 :: _ when isdigit c1 ->
                let (rest, fVal) = scFloat(tail, 0.0, 0.0, 0.0)
                (rest, Num (F fVal))
            | _ ->
                raise parseError
        | _ ->
            let (afterInt, iVal) = scInt (chars, 0)
            match afterInt with
            | '.' :: tail ->
                let (rest, fVal) = scFloat (tail, float iVal, 0.0, 0.0)
                (rest, Num (F fVal))
            | _ ->
                (afterInt, Num (I iVal))
*)
    let lexer input = 
        let rec scan input =
            match input with
            | [] -> []
            | '+'::tail -> Add :: scan tail
            | '-'::tail -> Sub :: scan tail
            | '*'::tail -> Mul :: scan tail
            | '/'::tail -> Div :: scan tail
            | '%'::tail -> Rem :: scan tail 
            | '^'::tail -> Pow :: scan tail 
            | '('::tail -> Lpar:: scan tail
            | ')'::tail -> Rpar:: scan tail
            | '='::tail -> Assign :: scan tail 
            | ';'::tail -> Semi :: scan tail 
            | c :: tail when isblank c -> scan tail
            | c :: tail when isidstart c ->
                let (rest, name) = scanId tail (string c)
                match name with
                | "int"     -> VarInt       :: scan rest
                | "float"   -> VarFloat     :: scan rest
                | "sin"     -> Sin          :: scan rest
                | "cos"     -> Cos          :: scan rest
                | "tan"     -> Tan          :: scan rest
                | "exp"     -> Exp          :: scan rest
                | "log"     -> Log          :: scan rest
                | "ln"      -> Ln           :: scan rest
                | "pi"      -> Pi           :: scan rest
                | "e"       -> NaturalLogE  :: scan rest
                | _         -> Id name      :: scan rest

            (*
            | 'e'::tail when 
                (match tail with
                | c::_ when isalpha c -> false 
                | _ -> true) ->
                NaturalLogE:: scan tail
            
            | 's' :: 'i' :: 'n' :: tail when
                (match tail with | c :: _ when isalpha c -> false | _ -> true ) ->
                Sin :: scan tail 
            | 'c' :: 'o' :: 's' :: tail when
                (match tail with | c :: _ when isalpha c -> false | _ -> true ) ->
                Cos :: scan tail 
            | 't' :: 'a' :: 'n' :: tail when
                (match tail with | c :: _ when isalpha c -> false | _ -> true ) ->
                Tan :: scan tail 
            | 'p' :: 'i' :: tail when
                (match tail with | c :: _ when isalpha c -> false | _ -> true ) ->
                Pi :: scan tail 
            *)
            | c :: tail when isdigit c -> 
                let (iStr, iVal) = scInt(tail, intVal c) 
                match iStr with
                | '.'::nextc::nextTail when isdigit nextc ->
                    let (fStr, fVal) = scFloat(nextTail, iVal, float(intVal nextc), 1.0)
                    Num (F fVal):: scan fStr
                | nextc :: _ when isalpha nextc || nextc = '_' ->
                    raise parseError
                | _ -> 
                    Num (I iVal) :: scan iStr
            | _ -> raise lexError
        scan (str2lst input)

    let getInputString() : string = 
        Console.Write("Enter an expression: ")
        Console.ReadLine()

    // Grammar in BNF:
    // <Program>        ::= { <Statement> [";"] }
    // <Statement>      ::= <Declaration> | <Assign> | <E>
    // <Declaration>    ::= "int" Id [ "="<E> ] || "float" Id [ "="<E> ] 
    // <Assign>         ::= Id "=" <E>
    // <E>              ::= <T> <Eopt>
    // <Eopt>           ::= "+" <T> <Eopt> | "-" <T> <Eopt> | <empty>
    // <T>              ::= <P> <Topt>
    // <Topt>           ::= "*" <P> <Topt> | "/" <P> <Topt> | "%" <P> <Topt> | <empty>
    // <P>              ::= <NR> <Popt>
    // <Popt>           ::= "^" <NR> <Popt> | <empty>
    // <NR>             ::= "Num"/"NaturalLogE"/"pi" <value> | "+"/"-" "Num"/"NaturalLogE"/"Pi" <value> | "(" <E> ")"  | "+"/"-" "(" <E> ")"
    //                      | "+"/"-" "sin"/"cos"/"tan"/"exp"/"log"/"ln" "(" <E> ")"

    let parser tList= 
        
        let rec Program tList = 
            match tList with
            | [] -> []
            | _ ->
                let afterState = Statement tList
                let rest = 
                    match afterState with
                    | Semi :: tail -> tail
                    | _            -> afterState
                Program rest
        and Statement tList =
            match tList with
            | VarInt :: Id _ :: Assign :: tail -> E tail
            | VarInt :: Id _ :: tail -> tail
            | VarFloat :: Id _ :: Assign :: tail -> E tail
            | VarFloat :: Id _ :: tail -> tail
            | Id _ :: Assign :: tail -> E tail
            | _ -> E tList
        and E tList = (T >> Eopt) tList         // >> is forward function composition operator: let inline (>>) f g x = g(f x)
        and Eopt tList = 
            match tList with
            | Add :: tail -> (T >> Eopt) tail
            | Sub :: tail -> (T >> Eopt) tail
            | _ -> tList
        and T tList = (P >> Topt) tList
        and Topt tList =
            match tList with
            | Mul :: tail -> (P >> Topt) tail
            | Div :: tail -> (P >> Topt) tail
            | Rem :: tail -> (P >> Topt) tail
            | _ -> tList
        and P tList = (NR >> Popt) tList
        and Popt tList =
            match tList with
            | Pow :: tail -> (NR >> Popt) tail
            | _ -> tList
        and NR tList =
            match tList with 
            | Num value :: tail -> tail
            | Sub :: Num value :: tail -> tail 
            | Add :: Num value :: tail -> tail
            | NaturalLogE  :: tail -> tail
            | Sub :: NaturalLogE  :: tail -> tail
            | Add :: NaturalLogE  :: tail -> tail
            | Pi :: tail -> tail
            | Add :: Pi :: tail -> tail
            | Sub :: Pi :: tail -> tail
            | Id _ :: tail -> tail
            | Add :: Id _ :: tail -> tail
            | Sub :: Id _ :: tail -> tail
            | Lpar :: tail -> match E tail with 
                              | Rpar :: tail -> tail
                              | _ -> raise parseError
            | Sub :: Lpar :: tail -> match E tail with 
                                     | Rpar :: tail -> tail
                                     | _ -> raise parseError
            | Add :: Lpar :: tail -> match E tail with 
                                     | Rpar :: tail -> tail
                                     | _ -> raise parseError
            | Sin :: Lpar :: tail -> match E tail with 
                                     | Rpar :: tail -> tail
                                     | _ -> raise parseError
            | Sub :: Sin :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Add :: Sin :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Cos :: Lpar :: tail -> match E tail with 
                                     | Rpar :: tail -> tail
                                     | _ -> raise parseError
            | Sub :: Cos :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Add :: Cos :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Tan :: Lpar :: tail -> match E tail with 
                                     | Rpar :: tail -> tail
                                     | _ -> raise parseError
            | Sub :: Tan :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Add :: Tan :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Exp :: Lpar :: tail -> match E tail with 
                                     | Rpar :: tail -> tail
                                     | _ -> raise parseError
            | Sub :: Exp :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Add :: Exp :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Log :: Lpar :: tail -> match E tail with 
                                     | Rpar :: tail -> tail
                                     | _ -> raise parseError
            | Sub :: Log :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Add :: Log :: Lpar :: tail -> match E tail with 
                                            | Rpar :: tail -> tail
                                            | _ -> raise parseError
            | Ln :: Lpar :: tail -> match E tail with 
                                    | Rpar :: tail -> tail
                                    | _ -> raise parseError
            | Sub :: Ln :: Lpar :: tail -> match E tail with 
                                           | Rpar :: tail -> tail
                                           | _ -> raise parseError
            | Add :: Ln :: Lpar :: tail -> match E tail with 
                                           | Rpar :: tail -> tail
                                           | _ -> raise parseError
            | _ -> raise parseError
        match Program tList with
        | [] -> []
        | _ -> raise parseError

    let parseNeval tList env = 
        let rec Program (tList,env,lastVal) = 
            match tList with
            | [] -> ([],env,lastVal)
            | _ ->
                let (afterState ,env',lastVal') = Statement(tList,env,lastVal)
                let rest = 
                    match afterState with
                    | Semi :: tail -> tail
                    | _            -> afterState
                Program(rest,env',lastVal')
        and Statement(tList,env,lastVal) =
            match tList with
            | VarInt :: Id name :: Assign :: tail -> 
                let (rest,value) = E tail env
                let env' = declareInt name (Some value) env
                (rest,env',Some value)

            | VarInt :: Id name :: tail -> 
                let env' = declareInt name None env
                (tail,env',lastVal)

            | VarFloat :: Id name :: Assign :: tail -> 
                let (rest,value) = E tail env
                let env' = declareFloat name (Some value) env
                (rest,env',Some value)

            | VarFloat :: Id name :: tail -> 
                let env' = declareFloat name None env
                (tail,env',lastVal)

            | Id name :: Assign :: tail ->
                let (rest, value) = E tail env
                let env' = assignId name value env
                (rest, env' , Some value)
            | _ -> 
                let (rest,value) = E tList env
                (rest, env, Some value)

        and E tList env = 
            let (tLst, tVal) =  T tList env
            Eopt (tLst, env) tVal
        and Eopt (tList,env) value = 
            match tList with
            | Add :: tail -> let (tLst, tval) = T tail env
                             Eopt (tLst,env) (Value.add value tval)
            | Sub :: tail -> let (tLst, tval) = T tail env 
                             Eopt (tLst,env) (Value.sub value tval)
            | _ -> (tList, value)
        and T tList env = 
            let (tLst, pVal) = P tList env
            Topt (tLst, env) pVal
        and Topt (tList,env) value =
            match tList with
            | Mul :: tail -> let (tLst, tval) = P tail env
                             Topt (tLst,env) (Value.mul value tval)
            | Div :: tail -> let (tLst, tval) = P tail env
                             Topt (tLst,env) (Value.div value tval)
            | Rem :: tail -> let (tLst, tval) = P tail env
                             Topt (tLst,env) (Value.rem value tval)
            | _ -> (tList, value)
        and P tList env = 
            let (tLst,nrVal) = NR tList env
            Popt (tLst,env) nrVal
        and Popt (tList,env) value =
            match tList with
            | Pow :: tail -> let (tLst, tval) = NR tail env 
                             Popt (tLst,env) (Value.pow value tval )
            | _ -> (tList, value)
        and NR tList env =
            match tList with 
            | Num value :: tail -> (tail, value)
            | Sub :: Num value :: tail -> (tail, Value.negate value)
            | Add :: Num value :: tail -> (tail, value)
            | NaturalLogE  :: tail -> (tail, F System.Math.E)
            | Sub :: NaturalLogE  :: tail -> (tail, Value.negate ( F System.Math.E ) )
            | Add :: NaturalLogE  :: tail -> (tail, F System.Math.E)
            | Pi  :: tail -> (tail, F System.Math.PI)
            | Sub :: Pi  :: tail -> (tail, Value.negate ( F System.Math.PI ) )
            | Add :: Pi  :: tail -> (tail, F System.Math.PI)
            | Id name :: tail ->
                let value = valueOfId name env
                (tail, value)
            | Sub :: Id name :: tail ->
                let value = valueOfId name env |> Value.negate
                (tail, value)
            | Add :: Id name :: tail ->
                let value = valueOfId name env
                (tail, value)
            | Lpar :: tail -> let (tLst, tval) = E tail env
                              match tLst with 
                              | Rpar :: tail -> (tail, tval)
                              | _ -> raise parseError
            | Sub :: Lpar :: tail -> let (tLst, tval) = E tail env
                                     match tLst with 
                                     | Rpar :: tail -> (tail, Value.negate tval)
                                     | _ -> raise parseError
            | Add :: Lpar :: tail -> let (tLst, tval) = E tail env
                                     match tLst with 
                                     | Rpar :: tail -> (tail, tval)
                                     | _ -> raise parseError
            | Sin :: Lpar :: tail -> let (tLst, tval) = E tail env
                                     match tLst with 
                                     | Rpar :: tail -> (tail, Value.sinv tval)
                                     | _ -> raise parseError
            | Sub :: Sin :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.negate( Value.sinv tval ) )
                                            | _ -> raise parseError
            | Add :: Sin :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.sinv tval)
                                            | _ -> raise parseError
            | Cos :: Lpar :: tail -> let (tLst, tval) = E tail env
                                     match tLst with 
                                     | Rpar :: tail -> (tail, Value.cosv tval)
                                     | _ -> raise parseError
            | Sub :: Cos :: Lpar :: tail -> let (tLst, tval) = E tail env 
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.negate( Value.cosv tval ) )
                                            | _ -> raise parseError
            | Add :: Cos :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.cosv tval)
                                            | _ -> raise parseError
            | Tan :: Lpar :: tail -> let (tLst, tval) = E tail env
                                     match tLst with 
                                     | Rpar :: tail -> (tail, Value.tanv tval)
                                     | _ -> raise parseError
            | Sub :: Tan :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.negate( Value.tanv tval ) )
                                            | _ -> raise parseError
            | Add :: Tan :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.tanv tval)
                                            | _ -> raise parseError
            | Exp :: Lpar :: tail -> let (tLst, tval) = E tail env
                                     match tLst with 
                                     | Rpar :: tail -> (tail, Value.expv tval)
                                     | _ -> raise parseError
            | Sub :: Exp :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.negate( Value.expv tval ) )
                                            | _ -> raise parseError
            | Add :: Exp :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.expv tval)
                                            | _ -> raise parseError
            | Log :: Lpar :: tail -> let (tLst, tval) = E tail env
                                     match tLst with 
                                     | Rpar :: tail -> (tail, Value.log10v tval)
                                     | _ -> raise parseError
            | Sub :: Log :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.negate( Value.log10v tval ) )
                                            | _ -> raise parseError
            | Add :: Log :: Lpar :: tail -> let (tLst, tval) = E tail env
                                            match tLst with 
                                            | Rpar :: tail -> (tail, Value.log10v tval)
                                            | _ -> raise parseError
            | Ln :: Lpar :: tail -> let (tLst, tval) = E tail env
                                    match tLst with 
                                    | Rpar :: tail -> (tail, Value.lnv tval)
                                    | _ -> raise parseError
            | Sub :: Ln :: Lpar :: tail -> let (tLst, tval) = E tail env
                                           match tLst with 
                                           | Rpar :: tail -> (tail, Value.negate( Value.lnv tval ) )
                                           | _ -> raise parseError
            | Add :: Ln :: Lpar :: tail -> let (tLst, tval) = E tail env
                                           match tLst with 
                                           | Rpar :: tail -> (tail, Value.lnv tval)
                                           | _ -> raise parseError
            | _ -> raise parseError
        Program(tList,env,None)

    let rec printTList (lst:list<terminal>) : list<string> = 
        match lst with
        head::tail -> Console.Write("{0} ",head.ToString())
                      printTList tail
                    
        | [] -> Console.Write("EOL\n")
                []

    let Calculate (expression: string) =
        try
            if String.IsNullOrWhiteSpace(expression) then
                "Error: Please enter an expression"
            else
                let tokens = lexer expression
                let presult = parser tokens
                let (_,_finalEnv,result) = parseNeval tokens emptyEnv
                let render = function 
                    | I i -> i.ToString()
                    | F f ->                   
                        if Double.IsInfinity f || Double.IsNaN f then
                            "Error: Invalid calculation result"
                        else      
                            let r = Math.Round(f,12)
                            if abs( r - Math.Round(r)) < 1e-12 then 
                                (Math.Round r).ToString(Globalization.CultureInfo.InvariantCulture)
                            else
                                r.ToString("0.############", Globalization.CultureInfo.InvariantCulture)
                match result with
                | Some v -> render v
                | None -> "Accepted"
        with
        | :? System.Exception as ex -> 
            match ex.Message with
            | msg when msg.Contains("Lexer error") -> "Error: The expression contains an unrecognised character."
            | msg when msg.Contains("Parser error") -> "Error: Incorrect expression format"
            | msg when msg.Contains("div zero") -> "Error: The divisor cannot be zero."
            | _ -> sprintf "Error: %s" ex.Message
        | _ -> "Error: An unknown error occurred during the calculation process."

    let Calculate_st (expression: string, symbolTable: Environment) : (string * Environment) =
        try
            if String.IsNullOrWhiteSpace(expression) then
                ("Error: Please enter an expression", symbolTable)
            else
                let tokens = lexer expression
                let (_, finalEnv, result) = parseNeval tokens symbolTable
                
                let render = function 
                    | I i -> i.ToString()
                    | F f ->                   
                        if Double.IsInfinity f || Double.IsNaN f then
                            "Error: Invalid calculation result"
                        else      
                            let r = Math.Round(f, 12)
                            if abs(r - Math.Round(r)) < 1e-12 then 
                                (Math.Round r).ToString(Globalization.CultureInfo.InvariantCulture)
                            else
                                r.ToString("0.############", Globalization.CultureInfo.InvariantCulture)
                
                match result with
                | Some v -> 
                    (render v, finalEnv)
                | None -> 
                    ("Accepted", finalEnv)
        with
        | :? System.Exception as ex -> 
            let errorMsg = 
                match ex.Message with
                | msg when msg.Contains("Lexer error") -> "Error: The expression contains an unrecognised character."
                | msg when msg.Contains("Parser error") -> "Error: Incorrect expression format"
                | msg when msg.Contains("div zero") -> "Error: The divisor cannot be zero."
                | _ -> sprintf "Error: %s" ex.Message
            (errorMsg, symbolTable)
        | _ -> ("Error: An unknown error occurred during the calculation process.", symbolTable)

    let GetSymbolTableData (symbolTable: Environment) =
        symbolTable
        |> Map.toList
        |> List.map (fun (name, varSym) ->
            let valueStr = 
                match varSym.Value with
                | None -> "Not set"
                | Some (I i) -> i.ToString()
                | Some (F f) -> 
                    if Double.IsInfinity f || Double.IsNaN f then
                        "Invalid"
                    else
                        let r = Math.Round(f, 12)
                        if abs(r - Math.Round(r)) < 1e-12 then 
                            (Math.Round r).ToString(Globalization.CultureInfo.InvariantCulture)
                        else
                            r.ToString("0.############", Globalization.CultureInfo.InvariantCulture)
            (name, valueStr)
        )



