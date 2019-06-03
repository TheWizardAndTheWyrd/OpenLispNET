using System;
using System.IO;
using System.Linq;
using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes;
using OpenLisp.Core.DataTypes.Errors;
using OpenLisp.Core.DataTypes.Errors.Throwable;
using OpenLisp.Core.Events.Args;

namespace OpenLisp.Core.StaticClasses
{
    /// <summary>
    /// Default REPL implementation for OpenLisp.NET
    /// </summary>
    public static class Repl
    {
        private static String _prompt = "wizard";

        /// <summary>
        /// Enable us to retrieve the default prompt value at any time.
        /// </summary>
        public static readonly String DefaultPrompt = _prompt;

        /// <summary>
        /// Get or set the REPL prompt.
        /// </summary>
        public static String Prompt
        {
            get
            {
                return _prompt;
            }
            set
            {
                _prompt = value;
            }
        }

        static void OnPrintEvent(object sender, PrintEventArgs e) => PrintEvent?.Invoke(sender, e);
        static void OnInputEvent(object sender, PrintEventArgs e) => InputEvent?.Invoke(sender, e);

        /// <summary>
        /// Printe Event handler.
        /// </summary>
        public static event EventHandler<PrintEventArgs> PrintEvent;

        /// <summary>
        /// Input Event handler.
        /// </summary>
        public static event EventHandler<PrintEventArgs> InputEvent; 

        /// <summary>
        /// Use the Reader to read a string and return an OpenLispVal.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static OpenLispVal Read(string str)
        {
            return Reader.ReadStr(str);
        }

        /// <summary>
        /// Is this <see cref="OpenLispVal"/> a pair?
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static bool IsPair(OpenLispVal x)
        {
            return x is OpenLispList && ((OpenLispList) x).Size > 0;
        }

        /// <summary>
        /// Implements quasi-quotes.
        /// 
        /// TODO: refactor to move to <see cref="OpenLisp.Core.StaticClasses.CoreNameSpace"/>.
        /// </summary>
        /// <param name="abstractSyntaxTree"></param>
        /// <returns></returns>
        public static OpenLispVal QuasiQuote(OpenLispVal abstractSyntaxTree)
        {
            if (!IsPair(abstractSyntaxTree))
            {
                return new OpenLispList(new OpenLispSymbol("quote"), abstractSyntaxTree);
            }

            OpenLispVal caputPrimus = ((OpenLispList)abstractSyntaxTree)[0];

            var symbol = caputPrimus as OpenLispSymbol;

            if ((symbol != null) && (symbol.ToString() == "unquote"))
            {
                return ((OpenLispList) abstractSyntaxTree)[1];
            }

            if (!IsPair(caputPrimus))
                return new OpenLispList(new OpenLispSymbol("cons"),
                    QuasiQuote(caputPrimus),
                    QuasiQuote(((OpenLispList) abstractSyntaxTree).Rest()));

            OpenLispVal caputSecundus = ((OpenLispList)caputPrimus)[0];

            var lispSymbol = caputSecundus as OpenLispSymbol;

            return (lispSymbol != null) && (lispSymbol.ToString() == "splice-unquote")
                ? new OpenLispList(new OpenLispSymbol("concat"),
                    ((OpenLispList) caputPrimus)[1],
                    QuasiQuote(((OpenLispList) abstractSyntaxTree).Rest()))
                : new OpenLispList(new OpenLispSymbol("cons"),
                    QuasiQuote(caputPrimus),
                    QuasiQuote(((OpenLispList) abstractSyntaxTree).Rest()));
        }

        /// <summary>
        /// Is this <see cref="OpenLispVal"/> a macro call in the current <see cref="Env"/>?
        /// </summary>
        /// <param name="abstractSyntaxTree"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static bool IsMacroCall(OpenLispVal abstractSyntaxTree, Env environment)
        {
            var list = abstractSyntaxTree as OpenLispList;

            OpenLispVal caputPrimus = list?[0];

            if (!(caputPrimus is OpenLispSymbol) || environment.Find((OpenLispSymbol) caputPrimus) == null) return false;

            OpenLispVal macro = environment.Get((OpenLispSymbol)caputPrimus);

            var func = macro as OpenLispFunc;

            return func != null &&
                   func.Macro;
        }

        /// <summary>
        /// Expands a macro.
        ///
        /// TODO: refactor to move to <see cref="OpenLisp.Core.StaticClasses.CoreNameSpace"/>.
        /// </summary>
        /// <param name="abstractSyntaxTree"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static OpenLispVal MacroExpand(OpenLispVal abstractSyntaxTree, Env environment)
        {
            while (IsMacroCall(abstractSyntaxTree, environment))
            {
                OpenLispSymbol caputPrimus = (OpenLispSymbol)((OpenLispList)abstractSyntaxTree)[0];

                OpenLispFunc macro = (OpenLispFunc)environment.Get(caputPrimus);

                abstractSyntaxTree = macro.Apply(((OpenLispList)abstractSyntaxTree).Rest());
            }
            return abstractSyntaxTree;
        }

        /// <summary>
        /// Evalues an AST in the environment.
        /// 
        /// TODO: refactor to move to <see cref="OpenLisp.Core.StaticClasses.CoreNameSpace"/>.
        /// </summary>
        /// <param name="abstractSyntaxTree"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static OpenLispVal EvalAst(OpenLispVal abstractSyntaxTree, Env environment)
        {
            var key = abstractSyntaxTree as OpenLispSymbol;

            if (key != null)
            {
                return environment.Get(key);
            }

            var list = abstractSyntaxTree as OpenLispList;

            if (list == null)
            {
                var map = abstractSyntaxTree as OpenLispHashMap;
                if (map == null) return abstractSyntaxTree;
                var newDictionary =
                    map.Value.ToDictionary(
                        entry => entry.Key, entry => Eval(entry.Value, environment));
                return new OpenLispHashMap(newDictionary);
            }

            OpenLispList oldList = list;
            OpenLispList newList = abstractSyntaxTree.ListQ() ? new OpenLispList()
                : (OpenLispList)new OpenLispVector();

            foreach (OpenLispVal movedValue in oldList.Value)
            {
                newList.Conj(Eval(movedValue, environment));
            }
            return newList;
        }

        /// <summary>
        /// Evaluate an <see cref="OpenLispVal"/> inside an <seealso cref="Env"/>.
        /// 
        /// The core namespace is defined in <seealso cref="OpenLisp.Core.StaticClasses.CoreNameSpace"/>.
        /// 
        /// TODO: refactor the switch over treeHeadSymbol.  All symbols of the core language should be defined in the same place.
        /// </summary>
        /// <param name="originalAbstractSyntaxTree"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static OpenLispVal Eval(OpenLispVal originalAbstractSyntaxTree, Env environment)
        {
            while (true)
            {
                //Console.WriteLine("EVAL: " + printer._pr_str(orig_ast, true));
                if (!originalAbstractSyntaxTree.ListQ())
                {
                    return EvalAst(originalAbstractSyntaxTree, environment);
                }

                // apply list
                OpenLispVal expanded = MacroExpand(originalAbstractSyntaxTree, environment);
                if (!expanded.ListQ())
                {
                    return expanded;
                }

                OpenLispList abstractSyntaxTree = (OpenLispList)expanded;
                if (abstractSyntaxTree.Size == 0)
                {
                    return abstractSyntaxTree;
                }

                var treeHead = abstractSyntaxTree[0];

                var symbol = treeHead as OpenLispSymbol;
                String treeHeadSymbol = symbol?.ToString() ?? "__<*fn*>__";

                // Let's get alchemical in our metaphors:
                OpenLispVal caputPrimus;    // The First Head.  Here's a vector: [1 lol 2 3 apple].  caputPrimus should be: 1.
                OpenLispVal caputSecundus;  // The Second Head.  Here's a list: `(1 lol 2 3 apple).  caputSecundus should be: lol.
                OpenLispVal solutio;

                switch (treeHeadSymbol)
                {
                    // TODO: extract this switch out of the REPL and consolidate in the core NS.
                    case "def!":
                        caputPrimus = abstractSyntaxTree[1];
                        caputSecundus = abstractSyntaxTree[2];
                        solutio = Eval(caputSecundus, environment);
                        environment.Set((OpenLispSymbol)caputPrimus, solutio);
                        return solutio;
                    case "let*":
                        caputPrimus = abstractSyntaxTree[1];
                        caputSecundus = abstractSyntaxTree[2];
                        OpenLispSymbol key;
                        OpenLispVal value;
                        Env letEnvironment = new Env(environment);  // TODO: explain ramifications to memory allocation and protection by creating a new Env object this way.
                        for (int i = 0; i < ((OpenLispList)caputPrimus).Size; i += 2)
                        {
                            key = (OpenLispSymbol)((OpenLispList)caputPrimus)[i];
                            value = ((OpenLispList)caputPrimus)[i + 1];
                            letEnvironment.Set(key, Eval(value, letEnvironment));
                        }
                        originalAbstractSyntaxTree = caputSecundus;
                        environment = letEnvironment;
                        break;
                    case "quote":
                        return abstractSyntaxTree[1];
                    case "quasiquote":
                        originalAbstractSyntaxTree = QuasiQuote(abstractSyntaxTree[1]);
                        break;
                    case "defmacro!":
                        caputPrimus = abstractSyntaxTree[1];
                        caputSecundus = abstractSyntaxTree[2];
                        solutio = Eval(caputSecundus, environment);
                        ((OpenLispFunc)solutio).Macro = true;
                        environment.Set(((OpenLispSymbol)caputPrimus), solutio);
                        return solutio;
                    case "macroexpand":
                        caputPrimus = abstractSyntaxTree[1];
                        return MacroExpand(caputPrimus, environment);
                    case "try*":
                        try
                        {
                            return Eval(abstractSyntaxTree[1], environment);
                        }
                        catch (Exception caught)
                        {
                            if (abstractSyntaxTree.Size <= 2) throw caught;

                            OpenLispVal openLispException;

                            caputSecundus = abstractSyntaxTree[2];
                            OpenLispVal caputSecundusHead = ((OpenLispList)caputSecundus)[0];
                            if (((OpenLispSymbol) caputSecundusHead).ToString() != "catch*") throw caught;

                            var exception = caught as OpenLispException;
                            openLispException = exception != null
                                ? (OpenLispVal) exception.Value
#if TRACE
                                : new OpenLispString(caught.StackTrace);
#elif !TRACE
                                : new OpenLispString("Stack Trace not yet available in OS.");
#endif
                            return Eval(((OpenLispList)caputSecundus)[2],
                                new Env(environment, ((OpenLispList)caputSecundus).Slice(1, 2),
                                    new OpenLispList(openLispException)));
                        }
                    case "do":
                        EvalAst(abstractSyntaxTree.Slice(1, abstractSyntaxTree.Size - 1), environment);
                        originalAbstractSyntaxTree = abstractSyntaxTree[abstractSyntaxTree.Size - 1];
                        break;
                    case "if":
                        caputPrimus = abstractSyntaxTree[1];
                        OpenLispVal condition = Eval(caputPrimus, environment);
                        if (condition == StaticOpenLispTypes.Nil || condition == StaticOpenLispTypes.False)
                        {
                            // eval false slot form
                            if (abstractSyntaxTree.Size > 3)
                            {
                                originalAbstractSyntaxTree = abstractSyntaxTree[3];
                            }
                            else
                            {
                                return StaticOpenLispTypes.Nil;
                            }
                        }
                        else
                        {
                            // eval true slot form
                            originalAbstractSyntaxTree = abstractSyntaxTree[2];
                        }
                        break;
                    case "fn*":
                        OpenLispList ligarePrimus = (OpenLispList)abstractSyntaxTree[1];
                        OpenLispVal ligareSecundus = abstractSyntaxTree[2];
                        Env currentEnvironment = environment;
                        return new OpenLispFunc(ligareSecundus, environment, ligarePrimus,
                            arguments => Eval(ligareSecundus, new Env(currentEnvironment, ligarePrimus, arguments)));
                    default:
                        var element = (OpenLispList)EvalAst(abstractSyntaxTree, environment);
                        var f = (OpenLispFunc)element[0];
                        OpenLispVal functionAbstractSyntaxTree = f.Ast;
                        if (functionAbstractSyntaxTree != null)
                        {
                            originalAbstractSyntaxTree = functionAbstractSyntaxTree;
                            environment = f.GenEnv(element.Rest());
                        }
                        else
                        {
                            return f.Apply(element.Rest());
                        }
                        break;
                }

            }
        }

        /// <summary>
        /// Pretty-prints an <see cref="OpenLispVal"/>.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string Print(OpenLispVal expression)
        {
            return Printer.PrStr(expression, true);
        }

        /// <summary>
        /// Main entry point of the OpenLisp.NET REPL.  
        /// </summary>
        /// <param name="arguments"></param>
        public static void ReplMain(string[] arguments)
        {
            // TODO: extract this from the Repl?
            var replEnvironment = new Env(null);

            // TODO: extract this from the Repl?
            Func<string, OpenLispVal> Re = (string str) => Eval(Read(str), replEnvironment);

            // Load each OpenLispSymbol in the core name space
            foreach (var entry in CoreNameSpace.Ns)
            {
                replEnvironment.Set(new OpenLispSymbol(entry.Key), entry.Value);
            }

            // TODO: extract this from the Repl
            replEnvironment.Set(new OpenLispSymbol("eval"), 
                        new OpenLispFunc(a => Eval(a[0], replEnvironment)));

            int fileIndex = 0;
            if (arguments.Length > 0 && arguments[0] == "--raw")
            {
                ReadLine.Mode = ReadLine.ModeEnum.Raw;
                fileIndex = 1;
            }

            OpenLispList argv = new OpenLispList();
            for (int i = fileIndex + 1; i < arguments.Length; i++)
            {
                argv.Conj(new OpenLispString(arguments[i]));
            }
            replEnvironment.Set(new OpenLispSymbol("*ARGV*"), argv);

            #region core.oln
            // BEGIN core.oln: defined using the language itself
            //
            // TODO: move this to its own class or assembly?  They rely on Repl.Eval above.
            //
            // NOTE: This can look a bit confusing if you aren't comfortable with Lisp.  One way 
            // to learn is start reading Lisp code inside out.  Start with the deepest nested 
            // function call and unwind all the way to the left.  For example:
            // 
            // (def! not (fn* (a) (if a false true)))
            //
            // (if a false true)
            // If 'a' is false, return true.
            //
            // (fn* (a) (if a false true))
            // Define f(a) as: if 'a' is false, return true.
            //
            // (def! not (fn* (a) (if a false true)))
            // Define a symbol named 'not' which has a value of f(a) where f(a)
            // is defined as: if 'a' is false, return true.
            Re("(def! not (fn* (a) (if a false true)))");

            // (slurp f)
            // Read the string content of a file named 'f' from disk.
            //
            // \"(do \" (slurp f) \")\"
            // Read the string content of a file name 'f' from disk and interpolate 
            // the content between two strings: "(do " and ")" respectively.
            //
            // (str \"(do \" (slurp f) \")\")
            // Read the string content of a file name 'f' from disk and interpolate 
            // the content between two strings: "(do " and ")" respectively.  Store this
            // as an OpenLisp.NET str.
            //
            // (read-string (str \"(do \" (slurp f) \")\"))
            // Read the string content of a file name 'f' from disk and interpolate 
            // the content between two strings: "(do " and ")" respectively.  Store this
            // as an OpenLisp.NET str.  Invoke the function read-string.
            //
            // (eval (read-string (str \"(do \" (slurp f) \")\")))
            // Evaluate the slurped, interpolated, and read string as valid OpenLisp.NET source,
            // and "do" the commands in the valid OpenLisp.NET source.
            //
            // (fn* (f) (eval (read-string (str \"(do \" (slurp f) \")\"))))
            // Define a function f(f) to evaluate the slurped, interpolated, and read string as 
            // valid OpenLisp.NET source, and then executes the valid OpenLisp.NET source.
            //
            // (def! load-file (fn* (f) (eval (read-string (str \"(do \" (slurp f) \")\")))))            
            // Define a symbole named 'load-file' that can be called as a function with the form
            // f(f) that is defined as evaluating the slurped, interpolated, and read strings from
            // a file on disk as valid OpenLisp.NET source code that is directly invoked by the 
            // evaluated 'd' command surrounding the slurped file's contents.
            Re("(def! load-file (fn* (f) (eval (read-string (str \"(do \" (slurp f) \")\")))))");

            // Macros are a bit trickier:
            Re("(defmacro! cond (fn* (& xs) (if (> (count xs) 0) (list 'if (first xs) (if (> (count xs) 1) (nth xs 1) (throw \"odd number of forms to cond\")) (cons 'cond (rest (rest xs)))))))");

            // Macros are meta-programs that write code when called or invoked:
            Re("(defmacro! or (fn* (& xs) (if (empty? xs) nil (if (= 1 (count xs)) (first xs) `(let* (or_FIXME ~(first xs)) (if or_FIXME or_FIXME (or ~@(rest xs))))))))");
            // OpenLisp.NET, like a lot of Lisp dialects, is implicitly and explicitly homiconic.  This means that,
            // in practice, there is no difference between valid OpenLisp.NET data and valid OpenLisp.NET code.
            // This means we are able to construct new S-Expressions at run-time, and then use eval to interpret our
            // code.  A common technique is to build a string that is a lis of functions and parameters, and then
            // evaluate the string.  This is incredibly powerful in that running programs can modify themselves at
            // runtime.
            //
            // Macros exploit this awesome power to create code that write new code and valid source code at
            // program run-time, and these macros are exposed to the Engineer with the same calling conventions
            // as regular functions.
            //
            // END core.oln
            #endregion

            if (arguments.Length <= fileIndex)
            {
                while (true)
                {
                    string line;
                    try
                    {
                        // TODO: make user> reflect the current namespace.
                        //line = ReadLine.LineReader("user> ");
                        line = ReadLine.LineReader(String.Format("{0}> ", Repl.Prompt));
                        if (line != null)
                        {
                            if (line == "")
                            {
                                continue;
                            }
                        }
                        else
                        {
                            break;
                        }

                        OnInputEvent("USER", new PrintEventArgs(line));
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("IOException: " + e.Message);
                        break;
                    }

                    try
                    {
                        //Console.WriteLine(Print(Re(line)));
                        var p = Print(Re(line));
                        OnPrintEvent("REPL", new PrintEventArgs(p));
                        Console.WriteLine(p);
                    }
                    catch (OpenLispContinue)
                    {
                        continue;
                    }
                    catch (OpenLispException e)
                    {
                        Console.WriteLine("Error: " +
                                          //Printer.PrStr((OpenLispVal) e.Value, false));             // Implicitly call ToString()
                                          Printer.PrStr((OpenLispVal)e.Value.ToString(), false));     // Explicitly call ToString()

                        continue;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.Message);
#if TRACE
                        Console.WriteLine(e.StackTrace);
#elif !TRACE
                        Console.WriteLine("Stack Trace not yet available in OS.");
#endif
                        continue;
                    }
                }
            }
            else
            {
                Re("(load-file \"" + arguments[fileIndex] + "\")");
                return;
            }

            // repl loop
        }
    }
}