
namespace system.threading.tasks
{

    public class StackGuard
    {

        // the .Net StackGuard allows inlining of tasks up to some depth,
        // and then checks for sufficient stack space.  we have a simpler
        // version without the stack checks.

        private int m_inliningDepth = 0;
        private const int MAX_UNCHECKED_INLINING_DEPTH = 20;

        public bool TryBeginInliningScope()
        {
            if (m_inliningDepth < MAX_UNCHECKED_INLINING_DEPTH)
            {
                m_inliningDepth++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void EndInliningScope()
        {
            m_inliningDepth--;
            if (m_inliningDepth < 0)
                m_inliningDepth = 0;
        }

    }

}
