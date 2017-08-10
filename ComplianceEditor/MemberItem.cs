using System;

namespace TextEditor
{
    public class MemberItem : IComparable
    {
        public string _displayText;
        public object _tag;

        public int CompareTo(object obj)
        {
            int result = 1;
            if (obj != null)
            {
                if (obj is MemberItem)
                {
                    var memberItem = (MemberItem)obj;
                    return _displayText.CompareTo(memberItem._displayText);
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            return result;
        }
    }
}