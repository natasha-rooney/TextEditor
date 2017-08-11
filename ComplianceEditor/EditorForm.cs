using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Compliance.Intellisense;
using ComplianceEditor;

namespace Compliance.Editor
{
    public partial class EditorForm : Form
    {
        private bool[] _tabTextBoxModified;
        private string[] _openTabFilePaths;
        private int _closeRecDistanceFromLeft;
        private int _maxNumTabs = 10;
        private int _tabItemsDistanceFromTop;
        private Simple_Tables _simpleTablesInstance = new Simple_Tables();

        #region Initialisation Functions
        public EditorForm()
        {
            _tabTextBoxModified = new bool[_maxNumTabs];
            _openTabFilePaths = new string[_maxNumTabs];
            _tabTextBoxModified.Initialize();
            _openTabFilePaths.Initialize();

            InitializeComponent();
            InitializeTabControls();
            InitializeListViews();
            EnableListDragDropEvents();
        }

        private void InitializeTabControls()
        {
            textEditorTabControl.TabPages[0].Text = "<New File>";
            textEditorTabControl.Padding = new Point(7, 5);
            AddRichTextBox(textEditorTabControl, 0);

            toolsTabControl.TabPages[0].Text = "Tables";
            toolsTabControl.TabPages[1].Text = "Nominal";
        }

        private void InitializeListViews()
        {
            var tables = _simpleTablesInstance._tables;
            SetUpList(tables, tableListView, "Tables");

            var nominals = _simpleTablesInstance._nominalTable;
            SetUpList(nominals, nominalListView, "Nominals");
        }

        private void SetUpList(Dictionary<string, string> dictionary, ListView listView, string headerName)
        {
            listView.View = View.Details;
            listView.Scrollable = true;

            var header = new ColumnHeader()
            {
                Text = string.Empty,
                Name = headerName
            };

            listView.Columns.Add(header);

            foreach (var entry in dictionary)
            {
                listView.Items.Add(entry.Key.Capitalize(), entry.Value);
            }

            listView.Columns[0].Width = -2;
        }
        #endregion

        #region Textbox Functions
        private void AddRichTextBox(TabControl tabControl, int pageIndex)
        {
            var richTextBox = new RichTextBox()
            {
                Location = new Point(5, 5),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                AcceptsTab = true,
                Parent = tabControl.SelectedTab,

                Anchor =
                    AnchorStyles.Top
                    | AnchorStyles.Bottom
                    | AnchorStyles.Left
                    | AnchorStyles.Right,

                Size = new Size(
                    ClientSize.Width - 390,
                    ClientSize.Height - 137)
            };

            tabControl.TabPages[pageIndex].Controls.Add(richTextBox);
            richTextBox = EnableTextBoxDragDropEvents(tabControl, richTextBox);
            richTextBox.TextChanged += new EventHandler(TextBox_TextChanged);
        }

        private RichTextBox GetTabTextBox(TabControl tabControl)
        {
            return tabControl.SelectedTab.Controls.OfType<RichTextBox>().First();
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            var pageIndex = textEditorTabControl.SelectedIndex;

            if (!_tabTextBoxModified[pageIndex])
            {
                textEditorTabControl.TabPages[pageIndex].Text += "*";
                _tabTextBoxModified[pageIndex] = true;
            }
        }

        private void TextBoxTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            _closeRecDistanceFromLeft = e.Bounds.Right - 18;
            _tabItemsDistanceFromTop = e.Bounds.Top + 4;

            e.Graphics.DrawString(
                textEditorTabControl.TabPages[e.Index].Text,
                e.Font,
                Brushes.Black,
                e.Bounds.Left + 5,
                _tabItemsDistanceFromTop);

            e.DrawFocusRectangle();
        }
        #endregion

        #region Drag and Drop Setup Functions
        private void EnableListDragDropEvents()
        {
            tableListView.ItemDrag += new ItemDragEventHandler(TableListView_ItemDrag);
            nominalListView.ItemDrag += new ItemDragEventHandler(NominalListView_ItemDrag);
        }

        private RichTextBox EnableTextBoxDragDropEvents(TabControl tabControl, RichTextBox richTextBox)
        {
            richTextBox.DragEnter += new DragEventHandler(TextBox_DragEnter);
            richTextBox.DragDrop += new DragEventHandler(TextBox_DragDrop);
            richTextBox.AllowDrop = true;
            return richTextBox;
        }

        private void TableListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            tableListView.DoDragDrop(tableListView.SelectedItems, DragDropEffects.Move);
        }

        private void NominalListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            nominalListView.DoDragDrop(nominalListView.SelectedItems, DragDropEffects.Move);
        }

        private void TextBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void TextBox_DragDrop(object sender, DragEventArgs e)
        {
            var richTextBox = GetTabTextBox(textEditorTabControl);

            if (richTextBox == null)
            {
                MessageBox.Show("Could not drag and drop into textbox. Error: No text box has been provided.");
                return;
            }

            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection).ToString(), false))
            {
                var selectedListViewItems
                    = (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection));

                foreach (ListViewItem listItem in selectedListViewItems)
                {
                    var index = richTextBox.GetCharIndexFromPosition(richTextBox.PointToClient(Cursor.Position));
                    richTextBox.SelectionStart = index;
                    richTextBox.SelectionLength = 0;
                    richTextBox.SelectedText = listItem.ImageKey;
                }
            }
        }
        #endregion

        #region Open File Functions
        private void OpenButton_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog()
            {
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;

                if (_openTabFilePaths.Contains(fileName))
                {
                    textEditorTabControl.SelectedIndex = Array.IndexOf(_openTabFilePaths, fileName);
                }
                else
                {
                    OpenTab(fileName);
                    ReadFile(openFileDialog);
                    UpdateTextEditorTabText(textEditorTabControl.SelectedIndex, fileName);
                }
            }
        }

        private void ReadFile(OpenFileDialog openFileDialog)
        {
            var richTextBox = GetTabTextBox(textEditorTabControl);

            using (var streamReader = new StreamReader(openFileDialog.OpenFile()))
            {
                try
                {
                    var line = string.Empty;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        richTextBox.AppendText(line + "\n");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to load file. \nException: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region Save File Functions
        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(_openTabFilePaths[textEditorTabControl.SelectedIndex]);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveFile(_openTabFilePaths[textEditorTabControl.SelectedIndex]);
        }

        private void SaveAsButton_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }

        private void SaveFile(string fileName)
        {
            var pageIndex = textEditorTabControl.SelectedIndex;

            if (string.IsNullOrEmpty(fileName))
            {
                SaveFileAs();
                return;
            }
            else if (!_tabTextBoxModified[pageIndex])
            {
                return;
            }

            if (WriteToFile(fileName))
            {
                UpdateTextEditorTabText(pageIndex, fileName);
            }
        }

        private void SaveFileAs()
        {
            var pageIndex = textEditorTabControl.SelectedIndex;

            var saveFileDialog = new SaveFileDialog()
            {
                RestoreDirectory = true
            };

            if ((saveFileDialog.ShowDialog() == DialogResult.OK)
                && WriteToFile(saveFileDialog.FileName))
            {
                UpdateTextEditorTabText(pageIndex, saveFileDialog.FileName);
            }
        }

        private bool WriteToFile(string fileName)
        {
            using (var streamWriter = new StreamWriter(fileName)) // Got error: file cannot be null
            {
                try
                {
                    streamWriter.Write(GetTabTextBox(textEditorTabControl).Text);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to save file. \nException: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }
            }
        }
        #endregion

        #region Text Editor Tab Functions
        private void NewTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenTab("<New File>");
        }

        private void OpenTab(string fileName)
        {
            var tabPages = textEditorTabControl.TabPages;

            if (textEditorTabControl.TabPages.Count < _maxNumTabs)
            {
                var tabPage = new TabPage()
                {
                    BackColor = Color.White
                };

                tabPages.Add(tabPage);
                textEditorTabControl.SelectedTab = tabPage;
                textEditorTabControl.DrawItem += new DrawItemEventHandler(TextBoxTabControl_DrawItem);

                UpdateTextEditorTabText(textEditorTabControl.SelectedIndex, fileName);
                AddRichTextBox(textEditorTabControl, tabPages.Count - 1);
            }
            else
            {
                MessageBox.Show(
                    "You already have the maximum number of tabs open.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void CloseTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var closingTabPageIndex = textEditorTabControl.SelectedIndex;
            var tabPages = textEditorTabControl.TabPages;
            CloseTab(textEditorTabControl);

            if (textEditorTabControl.TabPages.Count > 0)
            {
                if (closingTabPageIndex != 0)
                {
                    textEditorTabControl.SelectedTab = tabPages[closingTabPageIndex - 1];
                }
                else
                {
                    textEditorTabControl.SelectedTab = tabPages[0];
                }
            }
        }

        private void CloseTab(TabControl tabControl)
        {
            var closingTabPageIndex = tabControl.SelectedIndex;

            if (_tabTextBoxModified[closingTabPageIndex])
            {
                var saveChanges = MessageBox.Show(
                            "You have unsaved changes. Do you want to save them first?",
                            "Confirm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                if (saveChanges == DialogResult.Yes)
                {
                    SaveFile(_openTabFilePaths[closingTabPageIndex]);
                }
                else if (saveChanges == DialogResult.Cancel)
                {
                    return;
                }
            }

            tabControl.TabPages.RemoveAt(closingTabPageIndex);
            _openTabFilePaths[closingTabPageIndex] = null;
            _tabTextBoxModified[closingTabPageIndex] = false;

            for (int i = closingTabPageIndex; i < tabControl.TabPages.Count; i++)
            {
                _openTabFilePaths[i] = _openTabFilePaths[i + 1];
                _tabTextBoxModified[i] = _tabTextBoxModified[i + 1];
            }
        }

        private void UpdateTextEditorTabText(int pageIndex, string fileName)
        {
            var fileNameWithoutExtension = string.Empty;

            try
            {
                fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            }
            catch (Exception)
            {
                fileNameWithoutExtension = fileName;
            }

            textEditorTabControl.TabPages[pageIndex].Text = fileNameWithoutExtension;
            SetUpTabArrayItems(pageIndex, fileName);
        }

        private void SetUpTabArrayItems(int pageIndex, string fileName)
        {
            _tabTextBoxModified[pageIndex] = false;
            _openTabFilePaths[pageIndex] = fileName;
        }
        #endregion

        // TODO: Update drag and drop menus
    }
}
