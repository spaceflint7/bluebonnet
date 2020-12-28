
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public class JavaStackMap
    {

        internal class Frame
        {
            internal List<JavaType> locals;
            internal List<JavaType> stack;
            internal bool branchTarget;
            internal Frame() {}
        }



        Dictionary<ushort, Frame> frames;
        Frame top;
        int curStack, maxStack;



        public JavaStackMap()
        {
            frames = new Dictionary<ushort, Frame>();
            top = new Frame();
            top.locals = new List<JavaType>();
            top.stack = new List<JavaType>();
        }



        public int[] SaveFrame(ushort offsetLabel, bool branchTarget,
                               JavaException.Where Where)
        {
            #if false
            {
            Console.WriteLine("-----------> Save Frame at " + offsetLabel.ToString("X4") + " branchTarget " + branchTarget);
                var (old1, old2) = FrameToString(offsetLabel);
                var (top1, top2) = FrameToString();
                Console.WriteLine("old frame = " + old1 + " ; " + old2);
                Console.WriteLine("top frame = " + top1 + " ; " + top2);
            Console.WriteLine(new System.Diagnostics.StackTrace(true).ToString());
            Console.WriteLine("<--------------");
            }
            #endif

            List<JavaType> copyLocals = null;
            List<JavaType> copyStack = null;
            int[] resetLocals = null;

            if (frames.TryGetValue(offsetLabel, out var old))
            {
                if (branchTarget)
                {
                    // we are saving a frame that is the target of a branch,
                    // so merging should not affect the current stack frame.
                    copyLocals = new List<JavaType>(top.locals);
                    copyStack = new List<JavaType>(top.stack);
                }

                resetLocals = MergeFrame(old, offsetLabel, Where);

                if (branchTarget)
                    old.branchTarget = true;
            }
            else
            {
                top.branchTarget = branchTarget;
                frames.Add(offsetLabel, top);
            }

            var newTop = new Frame();
            newTop.locals = copyLocals ?? new List<JavaType>(top.locals);
            newTop.stack = copyStack ?? new List<JavaType>(top.stack);
            top = newTop;

            return resetLocals;
        }



        public bool LoadFrame(ushort offsetLabel, bool keepStack,
                              JavaException.Where Where)
        {
            if (frames.TryGetValue(offsetLabel, out var frame))
            {
                top.locals = new List<JavaType>(frame.locals);
                if (keepStack)
                {
                    top.stack = new List<JavaType>(frame.stack);
                    curStack = 0;
                    foreach (var e in top.stack)
                        curStack += e.Category;
                }
                else
                    top.stack.Clear();
                return true;
            }
            else if (Where != null)
            {
                throw Where.Exception($"missing stack map at offset {offsetLabel:X4}");
            }
            return false;
        }



        int[] MergeFrame(Frame old, ushort offsetLabel, JavaException.Where Where)
        {
            //
            // compare locals, with the exception that a 'top' type (in the
            // sense of an uninitialized local), which was already recorded
            // in one frame, may override a non-'top' type in the other
            // frame.  this is the result of branching over
            //
            // ifnonnull L1     local 7 is uninitialized/'top' here
            // aload_1          assume local 1 is float
            // astore_7         local 7 is now initialized as float
            // L1: ...          conflicting frames, and we keep 'top'
            //
            // note:  if either frame contains more locals than the other
            // frame, we treat the missing locals as if they were defined
            // as a 'top' type (i.e. uninitialized) in the other frame.
            //

            List<int> resetLocals = null;

            var topLocals = top.locals;
            var oldLocals = old.locals;

            int topLocalsCount = topLocals.Count;
            int oldLocalsCount = oldLocals.Count;
            bool same = true;

            int iTop = 0;
            int iOld = 0;
            while (iTop < topLocalsCount || iOld < oldLocalsCount)
            {
                var oldElem = (iOld < oldLocalsCount) ? oldLocals[iOld] : Top;
                var topElem = (iTop < topLocalsCount) ? topLocals[iTop] : Top;

                if (! topElem.Equals(oldElem))
                {
                    if (oldElem.Equals(Top))
                        topLocals[iTop] = topElem = Top;
                    else if (topElem.Equals(Top))
                    {
                        oldLocals[iOld] = oldElem = Top;
                        // report locals that were reset during merge
                        if (resetLocals == null)
                            resetLocals = new List<int>();
                        resetLocals.Add(iOld);
                    }

                    else if (! topElem.AssignableTo(oldElem))
                    {
                        same = false;
                        break;
                    }
                }

                iTop += topElem.Category;
                iOld += oldElem.Category;
            }

            //
            // compare stacks, with the exception that a 'null' type, which
            // appears in one frame, may be overridden by a more specific
            // class type from the other frame, e.g.
            //
            // ifnonnull L1     stack is empty
            // aconst_null      stack is [ null ]
            // goto L2
            // L1.  aload_1     if local 1 is string, stack is [ string ]
            // L2.  ...         conflicting frames, and we keep [ string ]
            //
            // note that the result would be the same even if 'aconst_null'
            // and 'aload_1'.  and also note that 'uninitializedThis' is
            // treated in the same way as 'null'.
            //
            // a second exception is for assignability through the virtual
            // call to JavaType.AssignableTo.  if an element from a stack is
            // assignable to the othe stack, then the other stack overrides.
            // for example, any reference can be assigned to java.lang.Object,
            // so a conflict is resolved by overriding with java.lang.Object.
            //

            var topStack = top.stack;
            var oldStack = old.stack;

            int numStack = topStack.Count;
            same &= (numStack == oldStack.Count);

            if (same)
            {
                for (int i = 0; i < numStack; i++)
                {
                    var oldElem = oldStack[i];
                    var topElem = topStack[i];

                    if (! topElem.Equals(oldElem))
                    {
                        if (oldElem.Equals(Null) || oldElem.Equals(UninitializedThis))
                            oldStack[i] = topElem;

                        else if (topElem.Equals(Null) || topElem.Equals(UninitializedThis))
                            topStack[i] = oldElem;

                        else if (oldElem.AssignableTo(topElem))
                            oldStack[i] = topElem;

                        else if (topElem.AssignableTo(oldElem))
                            topStack[i] = oldElem;

                        else if (old.branchTarget &&
                                    (topElem = oldElem.ResolveConflict(topElem)) != null)
                        {
                            // the ternary operator ?: may produce code that causes a conflict:
                            //      object x = flag ? (object) new A() : (object) new B();
                            // if we detect such a conflict at a branch target, we assume this
                            // is the cause, and set the stack elements to java.lang.Object
                            oldStack[i] = topStack[i] = topElem;
                        }

                        else
                        {
                            same = false;
                            break;
                        }
                    }
                }
            }

            if (! same)
            {
                #if DEBUGDIAG
                var (old1, old2) = FrameToString(offsetLabel);
                var (top1, top2) = FrameToString();
                Console.WriteLine("old frame = " + old1 + " ; " + old2);
                Console.WriteLine("top frame = " + top1 + " ; " + top2);
                #endif
                throw Where.Exception($"conflicting stack frames at offset {offsetLabel:X4}");
            }

            return (resetLocals != null) ? resetLocals.ToArray() : null;
        }



        public bool HasFrame(ushort offset) => frames.ContainsKey(offset);
        public bool HasBranchFrame(ushort offset)
            => frames.TryGetValue(offset, out var frame) && frame.branchTarget;



        public void SetLocal(int index, JavaType type)
        {
            if (index < top.locals.Count)
                top.locals[index] = type;
            else
            {
                while (index > top.locals.Count)
                    top.locals.Add(Top);
                top.locals.Add(type);
            }
        }



        public JavaType GetLocal(int index)
        {
            if (index < top.locals.Count)
                return top.locals[index];
            else
                return Top;
        }



        public void PushStack(JavaType type)
        {
            top.stack.Add(type);
            curStack += type.Category;
            if (curStack > maxStack)
                maxStack = curStack;
        }



        public JavaType PopStack(JavaException.Where Where)
        {
            int n = top.stack.Count - 1;
            if (n < 0)
                throw Where.Exception("underflow in operand stack");
            var retval = top.stack[n];
            curStack -= retval.Category;
            top.stack.RemoveAt(n);
            return retval;
        }



        public void ClearStack()
        {
            top.stack.Clear();
            curStack = 0;
        }



        public JavaType[] StackArray()
        {
            return top.stack.ToArray();
        }



        public int GetMaxStackSize(JavaException.Where Where)
        {
            if (top.stack.Count != 0)
                throw Where.Exception($"operand stack is not empty: [ {string.Join(", ", top.stack)} ]");
            return maxStack;
        }



        public void ResetLocalsInFrame(ushort offset, int[] indexes)
        {
            if (frames.TryGetValue(offset, out var frame))
            {
                for (int i = 0; i < indexes.Length; i++)
                {
                    if (frame.locals.Count > indexes[i])
                    {
                        frame.locals[indexes[i]] = Top;
                    }
                }
            }
        }



        public void SetLocalInAllFrames(int index, JavaType type, JavaException.Where Where)
        {
            SetLocalInOneFrame(top, index, type, Where);
            foreach (var kvp in frames)
                SetLocalInOneFrame(kvp.Value, index, type, Where);

            static void SetLocalInOneFrame(Frame frm, int index, JavaType type,
                                           JavaException.Where Where)
            {
                if (index < frm.locals.Count)
                {
                    if (! frm.locals[index].Equals(type))
                    {
                        if (frm.locals[index] != Top)
                            throw Where.Exception($"local already assigned in stack frame");
                        frm.locals[index] = type;
                    }
                }
                else
                {
                    while (index > frm.locals.Count)
                        frm.locals.Add(Top);
                    frm.locals.Add(type);
                }
            }
        }



        public JavaStackMap(JavaAttribute.StackMapTable attr, JavaReader rdr)
        {
            List<JavaType> locals = null;
            frames = new Dictionary<ushort, Frame>();
            ushort offset = 0;

            for (int i = 0; i < attr.frames.Length; i++)
            {
                var type = attr.frames[i].type;
                var top = new Frame();

                offset += attr.frames[i].deltaOffset;
                if (i != 0)
                    offset++;

                if (locals == null || type == 255)
                    top.locals = new List<JavaType>();
                else
                    top.locals = new List<JavaType>(locals);

                top.stack = new List<JavaType>();

                if (attr.frames[i].locals != null)
                {
                    for (int j = 0; j < attr.frames[i].locals.Length; j++)
                    {
                        var localType =
                                VerificationTypeToJavaType(attr.frames[i].locals[j], rdr);
                        top.locals.Add(localType);
                        if (localType.Category == 2)
                            top.locals.Add(Category2);
                    }
                }
                else if (type >= 248 && type <= 250)
                {
                    int n = top.locals.Count;
                    while (type-- >= 248 && n-- > 0)
                        top.locals.RemoveAt(n);
                }

                if (attr.frames[i].stack != null)
                {
                    for (int j = 0; j < attr.frames[i].stack.Length; j++)
                        top.stack.Add(VerificationTypeToJavaType(attr.frames[i].stack[j], rdr));
                }

                frames.Add(offset, top);
                locals = top.locals;
            }
        }



        JavaType VerificationTypeToJavaType(JavaAttribute.StackMapTable.Slot slot, JavaReader rdr)
        {
            switch (slot.type)
            {
                case 0:
                    return Top;

                case 1:
                    return new JavaType(TypeCode.Int32, 0, null);

                case 2:
                    return new JavaType(TypeCode.Single, 0, null);

                case 3:
                    return new JavaType(TypeCode.Double, 0, null);

                case 4:
                    return new JavaType(TypeCode.Int64, 0, null);

                case 5:
                    return Null;

                case 6:
                    return UninitializedThis;

                case 7:
                    return rdr.ConstClass(slot.extra);

                case 8:
                    return new JavaType(TypeCode.DBNull, slot.extra, UninitializedNew.ClassName);

                default:
                    throw rdr.Where.Exception("invalid type in stack map frame");
            }
        }



        JavaAttribute.StackMapTable.Slot JavaTypeToVerificationType(JavaType type, JavaWriter wtr)
        {
            var slot = new JavaAttribute.StackMapTable.Slot();
            if (! type.IsReference)
            {
                slot.type = (byte) (                 type.IsIntLike ? 1
                          : (type.PrimitiveType == TypeCode.Single) ? 2
                          : (type.PrimitiveType == TypeCode.Double) ? 3
                          : (type.PrimitiveType == TypeCode.UInt64) ? 4
                          : (type.PrimitiveType == TypeCode.Int64)  ? 4
                          : throw wtr.Where.Exception("invalid type in stack map frame"));
            }
            else
            {
                switch (type.PrimitiveType)
                {
                    case TypeCode.DBNull when type.ClassName == Top.ClassName:
                        slot.type = 0;
                        break;

                    case TypeCode.DBNull when type.ClassName == Null.ClassName:
                        slot.type = 5;
                        break;

                    case TypeCode.DBNull when type.ClassName == UninitializedThis.ClassName:
                        slot.type = 6;
                        break;

                    case TypeCode.DBNull when type.ClassName == UninitializedNew.ClassName:
                        slot.type = 8;
                        slot.extra = (ushort) type.ArrayRank;
                        break;

                    default:
                        slot.type = 7;
                        slot.extra = wtr.ConstClass(type);
                        break;
                }
            }
            return slot;
        }



        public (string, string) FrameToString()
        {
            var strLocals = (top.locals.Count == 0) ? null
                          : string.Join(", ", top.locals);
            var strStack  = (top.stack.Count == 0) ? null
                          : string.Join(", ", top.stack);
            return (strLocals, strStack);
        }



        public (string, string) FrameToString(ushort offset)
        {
            if (frames.TryGetValue(offset, out var frame))
            {
                var strLocals = (frame.locals.Count == 0) ? null
                              : string.Join(", ", frame.locals);
                var strStack  = (frame.stack.Count == 0) ? null
                              : string.Join(", ", frame.stack);
                return (strLocals, strStack);
            }
            return (null, null);
        }



        public JavaAttribute.StackMapTable ToAttribute(
                    JavaWriter wtr, Dictionary<ushort, ushort> labelToOffsetMap)
        {
            if (frames.Count == 0)
                return null;

            var frames2 = new SortedDictionary<ushort, Frame>();
            foreach (var kvp in frames)
            {
                if (kvp.Value.branchTarget)
                {
                    var key = kvp.Key;
                    if (key != 0 || labelToOffsetMap.ContainsKey(0))
                    {
                        if (labelToOffsetMap.TryGetValue(key, out var offset))
                            key = offset;
                        if (frames2.ContainsKey(key))
                        {
                            throw wtr.Where.Exception($"multiple stack frames at offset {key:X4}");
                        }
                        frames2.Add(key, kvp.Value);
                    }
                }
            }

            ushort lastOffset = 0xFFFF;
            if (! frames.TryGetValue(0, out var lastFrame))
                throw wtr.Where.Exception("stackmap does not include frame for offset 0");

            var frames3 = new List<JavaAttribute.StackMapTable.Item>();
            foreach (var kvp in frames2)
            {
                ushort nextOffset = kvp.Key;
                Frame nextFrame = kvp.Value;
                frames3.Add(ToAttribute2(wtr, nextOffset, nextFrame, lastOffset, lastFrame));
                lastOffset = nextOffset;
                lastFrame = nextFrame;
            }

            var attr = new JavaAttribute.StackMapTable(frames3.ToArray());
            return attr;
        }



        JavaAttribute.StackMapTable.Item ToAttribute2(JavaWriter wtr, ushort offset, Frame frame,
                                                      ushort lastOffset, Frame lastFrame)
        {
            var item = new JavaAttribute.StackMapTable.Item();

            if (lastOffset != 0xFFFF)
                offset = (ushort) (offset - (lastOffset + 1));
            item.deltaOffset = offset;

            int numLocals = frame.locals.Count;
            var localsList = new List<JavaAttribute.StackMapTable.Slot>(numLocals);
            for (int i = 0; i < numLocals; i += frame.locals[i].Category)
                localsList.Add(JavaTypeToVerificationType(frame.locals[i], wtr));
            item.locals = localsList.ToArray();

            int numStack = frame.stack.Count;
            item.stack = new JavaAttribute.StackMapTable.Slot[numStack];
            for (int i = 0; i < numStack; i++)
                item.stack[i] = JavaTypeToVerificationType(frame.stack[i], wtr);

            item.type = 255;
            return item;
        }



        public static readonly JavaType Top =
                                    new JavaType(TypeCode.DBNull, 0, "./top");

        public static readonly JavaType Category2 =
                                    new JavaType(TypeCode.DBNull, 0, "(2nd)");

        public static readonly JavaType Null =
                                    new JavaType(TypeCode.DBNull, 0, "./null");

        public static readonly JavaType UninitializedThis =
                                    new JavaType(TypeCode.DBNull, 0, "./this");

        public static readonly JavaType UninitializedNew =
                                    new JavaType(TypeCode.DBNull, 0, "./new");

    }

}
