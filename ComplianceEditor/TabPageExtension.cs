using System.Windows.Forms;

namespace ComplianceEditor
{
    public class TabPageExtension : TabPage
    {
        private bool _textChangedSinceLastSave;
        private string _openFilePath;

        public bool TextChangedSinceLastSave
        {
            get { return _textChangedSinceLastSave; }
            set { _textChangedSinceLastSave = value; }
        }

        public string OpenFilePath
        {
            get
            {
                return _openFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _openFilePath = string.Empty;
                }
                else
                {
                    _openFilePath = value;
                }
            }
        }

        public TabPageExtension()
            : base()
        {
            _textChangedSinceLastSave = false;
            _openFilePath = string.Empty;
        }
    }
}
