using System.Drawing;
using System.Windows.Forms;

namespace Compliance.Editor
{
    public class GListBox : ListBox
    {
        private ImageList _myImageList;

        public GListBox()
        {
            this.DrawMode = DrawMode.OwnerDrawFixed;
        }

        public ImageList ImageList
        {
            get { return _myImageList; }
            set { _myImageList = value; }
        }

        protected override void OnDrawItem(System.Windows.Forms.DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawFocusRectangle();
            GListBoxItem item;
            Rectangle bounds = e.Bounds;
            Size imageSize = _myImageList.ImageSize;
            try
            {
                item = (GListBoxItem)Items[e.Index];
                if (item.ImageIndex != -1)
                {
                    _myImageList.Draw(e.Graphics, bounds.Left, bounds.Top, item.ImageIndex);
                    e.Graphics.DrawString(
                        item.Text,
                        e.Font,
                        new SolidBrush(e.ForeColor),
                        bounds.Left + imageSize.Width,
                        bounds.Top);
                }
                else
                {
                    e.Graphics.DrawString(
                        item.Text,
                        e.Font,
                        new SolidBrush(e.ForeColor),
                        bounds.Left,
                        bounds.Top);
                }
            }
            catch
            {
                if (e.Index != -1)
                {
                    e.Graphics.DrawString(
                        Items[e.Index].ToString(),
                        e.Font,
                        new SolidBrush(e.ForeColor),
                        bounds.Left,
                        bounds.Top);
                }
                else
                {
                    e.Graphics.DrawString(
                        Text,
                        e.Font,
                        new SolidBrush(e.ForeColor),
                        bounds.Left,
                        bounds.Top);
                }
            }

            base.OnDrawItem(e);
        }
    }
}
