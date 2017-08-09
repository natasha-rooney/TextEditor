namespace Compliance.Editor
{
    public class GListBoxItem
    {
        private string _myText;
        private int _myImageIndex;

        public GListBoxItem(string text, int index)
        {
            _myText = text;
            _myImageIndex = index;
        }

        public GListBoxItem(string text)
            : this(text, -1)
        {
        }

        public GListBoxItem()
            : this(string.Empty)
        {
        }

        public string Text
        {
            get { return _myText; }
            set { _myText = value; }
        }

        public int ImageIndex
        {
            get { return _myImageIndex; }
            set { _myImageIndex = value; }
        }

        public override string ToString()
        {
            return _myText;
        }
    }
}
