
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using IndentedText = SpaceFlint.JavaBinary.IndentedText;

public class DotNetPrinter
{

    public static void PrintType(IndentedText txt, TypeDefinition type)
    {
        /*if (type.HasCustomAttributes)
            PrintCustomAttributes(txt, type.CustomAttributes);*/

        txt.Write("/* {0:X} */ {1}{2}{3}{4}{5}{6} {7}",
            /* 0 */ type.Attributes,
            /* 1 */ (type.IsPublic ? "public " : string.Empty),
            /* 2 */ (type.IsAbstract ? "abstract " : string.Empty),
            /* 3 */ (type.IsSealed ? "sealed " : string.Empty),
            /* 4 */ ((type.IsAbstract && type.IsSealed) ? "/* static */ " : string.Empty),
            /* 5 */ (type.IsSerializable ? "/* serializable */ " : string.Empty),
            /* 6 */ (type.IsInterface ? "interface" : "class"),
            /* 7 */ type.FullName);

        var baseType = type.BaseType;
        /*if (baseType != null && baseType.FullName == "System.Object")
            baseType = null;*/

        if (baseType != null || type.HasInterfaces)
        {
            txt.Write(" : ");
            bool comma = false;

            if (baseType != null)
            {
                txt.Write(baseType.FullName);
                comma = type.HasInterfaces;
            }

            if (type.HasInterfaces)
            {
                foreach (var intrface in type.Interfaces)
                {
                    txt.Write("{0}{1}",
                        (comma ? ", " : string.Empty),
                        intrface.InterfaceType?.FullName ?? "(null)");

                    comma = true;
                }
            }
        }

        txt.Write(" {");
        txt.NewLine();
        txt.NewLine();
        txt.AdjustIndent(true);

        foreach (var nested in type.NestedTypes)
            PrintType(txt, nested);

        if (type.HasNestedTypes)
            txt.NewLine();

        foreach (var field in type.Fields)
            PrintField(txt, field);

        if (type.HasFields)
            txt.NewLine();

        foreach (var method in type.Methods)
            PrintMethod(txt, method);

        txt.AdjustIndent(false);
        txt.Write("}");
        txt.NewLine();
        txt.NewLine();
    }



    static void PrintField(IndentedText txt, FieldDefinition field)
    {
        byte[] initialValue = field.InitialValue;

        txt.Write("/* {0} */ {1}{2}{3}{4}{5} {6}{7}{8};",
            /* 0 */ ((uint)field.Attributes).ToString("X4"),
            /* 1 */ (field.IsPublic ? "public " :
                        (field.IsPrivate ? "private " :
                            (field.IsFamily ? "protected " :
                                string.Empty))),
            /* 2 */ (field.IsStatic ? "static " : string.Empty),
            /* 3 */ (field.IsInitOnly ? "readonly " : string.Empty),
            /* 4 */ (field.IsNotSerialized ? "/* transient */ " : string.Empty),
            /* 5 */ field.FieldType.FullName,
            /* 6 */ field.Name,
            /* 7 */ (initialValue.Length != 0 ? " = bytes { " : string.Empty),
            /* 8 */ (field.HasConstant ? $" = ({field.Constant?.GetType()}) {field.Constant ?? "null"}" : string.Empty));

        if (initialValue.Length != 0)
        {
            for (int i = 0; i < initialValue.Length; i++)
                txt.Write("{0:X2} ", initialValue[i]);
            txt.Write("};");
        }

        txt.NewLine();
    }



    static void PrintMethod(IndentedText txt, MethodDefinition method)
    {
        txt.Write("/* {0} */ {1}{2}{3}",
            /* 0 */ ((uint)method.Attributes).ToString("X4"),
            /* 1 */ (method.IsPublic ? "public " :
                        (method.IsPrivate ? "private " :
                            (method.IsFamily ? "protected " :
                                (method.IsAssembly ? "internal " :
                                    string.Empty)))),
            /* 2 */ (method.IsStatic ? "static " : string.Empty),
            /* 3 */ (method.IsVirtual ?
                        (method.IsNewSlot ? "virtual " : "override ")
                            : string.Empty));

        if (method.IsConstructor)
            txt.Write(method.DeclaringType.FullName);
        else
            txt.Write("{0} {1}", method.ReturnType.FullName, method.Name);

        txt.Write("(");

        int numArgs = 0;
        if (method.HasParameters)
        {
            bool comma = false;
            foreach (var arg in method.Parameters)
            {
                txt.Write("{0}{1} {2}",
                    /* 0 */ (comma ? ", " : string.Empty),
                    /* 1 */ arg.ParameterType.FullName,
                    /* 2 */ arg.Name);

                comma = true;
                numArgs++;
            }
        }

        txt.Write(")");
        txt.NewLine();

        if (method.HasOverrides)
        {
            txt.AdjustIndent(true);
            foreach (var m in method.Overrides)
            {
                txt.Write("overrides " + m);
                txt.NewLine();
            }
            txt.AdjustIndent(false);
        }

        if (method.HasBody)
        {
            txt.Write("{");
            txt.NewLine();
            txt.AdjustIndent(true);

            try
            {
                PrintMethodBody(txt, method.Body);
            }
            catch (Exception e)
            {
                foreach (var s in e.ToString().Split('\r', '\n'))
                {
                    if (! string.IsNullOrEmpty(s))
                    {
                        txt.Write(s);
                        txt.NewLine();
                    }
                }
            }

            txt.AdjustIndent(false);
            txt.Write("}");
        }

        txt.NewLine();
        txt.NewLine();
    }



    static void PrintMethodBody(IndentedText txt, MethodBody body)
    {
        var numVars = body.HasVariables ? body.Variables.Count : 0;
        var numArgs = body.Method.HasParameters ? body.Method.Parameters.Count : 0;

        txt.Write("// stack={0}, locals={1}, args_size={2}, code_size={3}",
            body.MaxStackSize, numVars, numArgs, body.CodeSize);
        txt.NewLine();
        txt.NewLine();

        var exceptions = new Dictionary<int, List<string>>();
        if (body.HasExceptionHandlers)
        {
            PrepareExceptions(body.ExceptionHandlers, exceptions);
            PrepareExceptions(null, exceptions);
        }

        foreach (var inst in body.Instructions)
        {
            if (exceptions.TryGetValue(inst.Offset, out var strs))
            {
                txt.AdjustIndent(3);
                foreach (var s in strs)
                {
                    txt.Write(s);
                    txt.NewLine();
                }
                txt.AdjustIndent(-3);
            }
            txt.Write(inst.ToString());
            txt.NewLine();
        }
    }



    static void PrepareExceptions(Mono.Collections.Generic.Collection<ExceptionHandler> excHandlers,
                                  Dictionary<int, List<string>> excStrings)
    {
        if (excHandlers == null)
        {
            foreach (var kvp in excStrings)
            {
                if (kvp.Value.Count == 0)
                    kvp.Value.Add("    }");
            }

            return;
        }

        var prevBlock = (First: -1, Last: -1);
        var currBlock = prevBlock;
        List<string> strs;

        foreach (var exc in excHandlers)
        {
            currBlock = (exc.TryStart.Offset, exc.TryEnd.Offset);

            if (currBlock != prevBlock)
            {
                if (! excStrings.TryGetValue(currBlock.First, out strs))
                    strs = new List<string>();
                strs.Insert(0, $"try {{ // {currBlock.First:X4}..{currBlock.Last:X4}");
                excStrings[currBlock.First] = strs;

                prevBlock = currBlock;
            }

            if (exc.HandlerStart != null)
            {
                if (! excStrings.TryGetValue(exc.HandlerStart.Offset, out strs))
                    strs = new List<string>();

                string str;

                if (exc.HandlerType == ExceptionHandlerType.Catch)
                    str = $"catch ({exc.CatchType.FullName})";

                else if (exc.HandlerType == ExceptionHandlerType.Filter)
                    str = "filter handler";

                else if (exc.HandlerType == ExceptionHandlerType.Finally)
                    str = "finally";

                else if (exc.HandlerType == ExceptionHandlerType.Fault)
                    str = "fault handler";

                else
                    str = "unknown handler";

                strs.Add($"    }} {str} in block {currBlock.First:X4}..{currBlock.Last:X4} {{");

                excStrings[exc.HandlerStart.Offset] = strs;
            }

            if (exc.FilterStart != null)
            {
                if (! excStrings.TryGetValue(exc.FilterStart.Offset, out strs))
                    strs = new List<string>();

                strs.Add($"    }} filter condition in block {currBlock.First:X4}..{currBlock.Last:X4} {{");

                excStrings[exc.FilterStart.Offset] = strs;
            }

            if (exc.HandlerEnd != null)
            {
                if (! excStrings.TryGetValue(exc.HandlerEnd.Offset, out strs))
                    excStrings[exc.HandlerEnd.Offset] = new List<string>();
            }

        }
    }



    /*static void PrintCustomAttributes(IndentedText txt,
                        Mono.Collections.Generic.Collection<CustomAttribute> customAttributes)
    {
        foreach (var attr in customAttributes)
        {
            txt.Write($"[{attr.AttributeType.FullName}]");
            txt.NewLine();
        }
    }*/


}
