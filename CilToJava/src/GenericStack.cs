
using System;
using System.Collections.Generic;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public class GenericStack
    {

        internal class GenericStackItem
        {
            internal IGenericInstance GenericParent;
            internal GenericInstanceType GenericInstance;
            internal string Name;
            internal CilType Type;
            internal int Index;
            internal int GenericHighIndex;
        }

        List<GenericStackItem> items;



        public GenericStack(GenericStack oldStack = null)
        {
            if (oldStack == null)
                items = new List<GenericStackItem>();
            else
                items = new List<GenericStackItem>(oldStack.items);
        }



        public int Mark()
        {
            return items.Count;
        }



        public void Release(int mark)
        {
            int n = items.Count;
            if (n > mark)
                items.RemoveRange(mark, n - mark);
        }



        public CilType EnterType(TypeReference fromType)
        {
            var myType = CilType.From(fromType);

            if (fromType is ArrayType fromArrayType)
            {
                 EnterType(fromArrayType.ElementType);
            }
            else
            {
                if (fromType.HasGenericParameters)
                {
                    EnterGenericProvider(fromType, myType, -1);
                }
                if (fromType.IsGenericInstance)
                {
                    var genericInstance = (GenericInstanceType) fromType;
                    EnterGenericInstance(genericInstance, genericInstance.ElementType);
                }
            }

            return myType;
        }



        int EnterGenericProvider(IGenericParameterProvider provider, CilType owner, int parameterIndex)
        {
            int pos = 0;

            foreach (var genericElement in provider.GenericParameters)
            {
                var item = new GenericStackItem();
                item.Name = CilType.GenericParameterFullName(genericElement);

                ++pos;  // index always +1
                if (parameterIndex == -1)
                {
                    item.Index = -pos;
                    item.Type = owner;
                }
                else
                    item.Index = parameterIndex + pos;

                items.Add(item);
            }

            return pos;
        }



        void EnterGenericInstance(IGenericInstance instance, IGenericParameterProvider provider)
        {
            if (instance.HasGenericArguments)
            {
                foreach (var genericElement in provider.GenericParameters)
                {
                    var item = new GenericStackItem();
                    var genericArgument = instance.GenericArguments[genericElement.Position];
                    if (genericArgument is ArrayType genericArgumentAsArray)
                        item.GenericInstance = genericArgumentAsArray.ElementType as GenericInstanceType;
                    else
                        item.GenericInstance = genericArgument as GenericInstanceType;
                    item.GenericParent = instance;
                    item.Name = CilType.GenericParameterFullName(genericElement);
                    item.Type = CilType.From(genericArgument);
                    items.Add(item);
                }
            }
        }



        public CilMethod EnterMethod(MethodReference fromMethod)
        {
            var myMethod = CilMethod.From(fromMethod);
            var declaringType = fromMethod.DeclaringType;

            int parameterIndex = (myMethod.HasThisArg ? 1 : 0);
            foreach (var p in myMethod.Parameters)
                parameterIndex += p.Type.Category;

            if (myMethod.IsConstructor || myMethod.IsStatic)
            {
                if (declaringType.HasGenericParameters)
                {
                    parameterIndex += EnterGenericProvider(declaringType, null, parameterIndex);
                }
            }

            if (declaringType.IsGenericInstance)
            {
                var genericInstance = (GenericInstanceType) declaringType;
                EnterGenericInstance(genericInstance, genericInstance.ElementType);
            }

            if (fromMethod.HasGenericParameters)
            {
                EnterGenericProvider(fromMethod, null, parameterIndex);
            }

            if (fromMethod.IsGenericInstance)
            {
                var genericInstance = (GenericInstanceMethod) fromMethod;
                EnterGenericInstance(genericInstance, genericInstance.ElementMethod);
            }

            return myMethod;
        }



        public void EnterMethod(CilType type, JavaMethodRef method, bool instanceMethod)
        {
            //
            // we expect the method definition to contain the suffix type parameters
            // (added by CilMethod.ImportGenericParameters), so we subtract their
            // number to determine the index of the first type parameter.  if this
            // is an instance method, we add one to account for 'this' argument.
            //

            var argumentIndex = method.Parameters.Count - type.GenericParameters.Count;
            if (instanceMethod)
                argumentIndex++;

            foreach (var genericParameter in type.GenericParameters)
            {
                var item = new GenericStackItem();
                item.Name = genericParameter.JavaName;
                item.Index = ++argumentIndex;   // index always +1
                items.Add(item);
            }
        }



        public (CilType, int) Resolve(string name)
        {
            #if DEBUGDIAG
            Console.WriteLine("Generic Resolve <" + name + ">");
            for (int i = 0; i < items.Count; i++)
                Console.WriteLine("Stack Item #" + i + " is " + items[i].Name + " Type " + items[i].Type + " Index " + items[i].Index);
            #endif

            var name0 = name;
            int n = items.Count;
            while (n-- > 0)
            {
                #if DEBUGDIAG
                //Console.WriteLine("Generic Stack Item #" + n + " is " + items[n].Name);
                #endif

                if (items[n].Name == name)
                {
                    var (type, index) = (items[n].Type, items[n].Index);

                    if (type == null || (! type.IsGenericParameter))
                    {
                        var genericInstance = items[n].GenericInstance;
                        if (genericInstance != null)
                        {
                            EnterGenericInstanceDuringResolve(genericInstance, n);
                        }

                        return (type, index);
                    }

                    name = type.JavaName;

                    // see EnterGenericInstanceDuringResolve
                    int n1 = items[n].GenericHighIndex;
                    if (n1 > 0 && n1 < n)
                        n = n1;
                }
            }
            throw CilMain.Where.Exception($"unresolvable generic type '{name0}'");
        }



        void EnterGenericInstanceDuringResolve(GenericInstanceType genericInstance, int index)
        {
            // consider method:  C<T,U>::F<S>() => new C<S,ValueTuple<T,U>>()
            // the generic stack initially contains mappings for T and U for
            // the 'this' instance of C.  upon entering 'newobj' instruction,
            // additional mappings for T and U are added.  so we have:
            //
            //      #0  C<T>            --> type field in 'this'
            //      #1  C<U>            --> type field in 'this'
            //      #2  F<S>            --> method parameter number
            //      #3  C<T>            --> F<S>
            //      #4  C<U>            --> ValueTuple<T1,T2>
            //      #5  ValueTuple<T1>  --> C<T>
            //      #6  ValueTuple<T2>  --> C<U>
            //
            // if resolution only takes generic names into account, entries
            // #5 and #6 cannot be resolved correctly:  #5 maps to F<S>, and
            // #6 maps back to itself via ValueTuple<T1,T2>.
            //
            // to address this, we track the highest value index in the stack
            // upon entering a generic instance, when the generic instance is
            // entered during Resolve().  in the example above, when entering
            // ValueTuple<T1,T2> (entry #4), we find the highest valid index
            // is entry #2, so #5 and #6 are resolved correctly as #0 and #1.
            //

            int highestIndex = 0;

            var genericParent = items[index].GenericParent;
            for (int j = 0; j < index; j++)
            {
                if (object.ReferenceEquals(items[j].GenericParent, genericParent))
                {
                    highestIndex = j;
                    break;
                }
            }

            int count1 = items.Count;
            EnterGenericInstance(genericInstance, genericInstance.ElementType);

            if (highestIndex != 0)
            {
                int count2 = items.Count;
                for (int j = count1; j < count2; j++)
                {
                    items[j].GenericHighIndex = highestIndex;
                }
            }
        }

    }

}
