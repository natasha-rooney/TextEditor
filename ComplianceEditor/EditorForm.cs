using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Compliance.Intellisense;
using System.Linq;
using System.Collections.Generic;
using TextEditor;
using System.Diagnostics;

namespace Compliance.Editor
{
    public partial class EditorForm : Form
    {
        private bool _changedSinceLastSave = false;
        private bool _foundNode = false;
        private bool _wordMatched = false;
        private int _closeRecDistanceFromLeft;
        private int _tabItemsDistanceFromTop;
        private string _currentPath;
        private string _openFilePath = string.Empty;
        private string _typed = string.Empty;

        private Assembly _assembly;
        private Hashtable _dictionaries;
        private Keys _keyLastPressed;
        private Simple_Tables _simpleTablesInstance = new Simple_Tables();
        private StatusBar _statusBar1;
        private TreeNode _findNodeResult = null;
        private TreeNode _nameSpaceNode;

        public EditorForm()
        {
            InitializeComponent();
            InitializeTabs();
            SetUpTab(textEditorTabControl, textEditorTabControl.TabPages.Count - 1);
            InitializeListViews();
            EnableDragDropEvents();
            ReadAssembly();
        }

        private void InitializeTabs()
        {
            textEditorTabControl.TabPages[0].Text = "<New File>";
            textEditorTabControl.DrawItem += new DrawItemEventHandler(TextEditorTabControl_DrawItem);
            textEditorTabControl.MouseDown += new MouseEventHandler(TextEditorTabControl_MouseDown);
            toolsTabControl.TabPages[0].Text = "Tables";
            toolsTabControl.TabPages[1].Text = "Nominal";
        }

        private void SetUpTab(TabControl tabControl, int pageIndex)
        {
            codeTextBox.KeyDown += new KeyEventHandler(RichTextBox_KeyDown);
            codeTextBox.MouseDown += new MouseEventHandler(RichTextBox_MouseDown);
            //AddRichTextBox(tabControl, pageIndex);
            AddGListBox(tabControl, pageIndex);
        }

        private void TextEditorTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            _closeRecDistanceFromLeft = e.Bounds.Left + 2;
            _tabItemsDistanceFromTop = e.Bounds.Top + 4;

            e.Graphics.DrawString(
                "x",
                e.Font,
                Brushes.Black,
                _closeRecDistanceFromLeft,
                _tabItemsDistanceFromTop);

            e.Graphics.DrawString(
                textEditorTabControl.TabPages[e.Index].Text,
                e.Font,
                Brushes.Black,
                e.Bounds.Left + 12,
                _tabItemsDistanceFromTop);

            e.DrawFocusRectangle();
        }

        private void TextEditorTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < textEditorTabControl.TabCount; i++)
            {
                var tabRecCloseButton = textEditorTabControl.GetTabRect(i);
                tabRecCloseButton.Offset(_closeRecDistanceFromLeft, _tabItemsDistanceFromTop);
                tabRecCloseButton.Width = 5;
                tabRecCloseButton.Height = 10;

                if (tabRecCloseButton.Contains(e.Location))
                {
                    if (!_changedSinceLastSave)
                    {
                        CloseTab(textEditorTabControl, i);
                    }
                    else
                    {
                        var saveChanges = MessageBox.Show(
                            "You have unsaved changes. Do you want to save before closing this tab?",
                            "Confirm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        if ((saveChanges == DialogResult.No) ||
                            ((saveChanges == DialogResult.Yes) && SaveFile(_openFilePath)))
                        {
                            CloseTab(textEditorTabControl, i);
                        }
                        else if (saveChanges == DialogResult.Cancel)
                        {
                            break;
                        }
                    }
                }
            }
        }

        #region RichTextBox
        private void AddRichTextBox(TabControl tabControl, int pageIndex)
        {
            var richTextBox = new RichTextBox();
            richTextBox.Name = "richTextBox" + pageIndex;
            richTextBox.Location = new System.Drawing.Point(5, 5);
            richTextBox.Size = new System.Drawing.Size(
                ClientSize.Width - 40,
                ClientSize.Height - 100);

            richTextBox.Anchor =
                AnchorStyles.Top
                | AnchorStyles.Bottom
                | AnchorStyles.Left
                | AnchorStyles.Right;

            tabControl.TabPages[pageIndex].Controls.Add(richTextBox);
            richTextBox.KeyDown += new KeyEventHandler(RichTextBox_KeyDown);
            richTextBox.MouseDown += new MouseEventHandler(RichTextBox_MouseDown);
        }

        private void RichTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            // Hide the listview and the tooltip
            textBoxTooltip.Hide();
            GetGListBox().Hide();
        }

        private void RichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var i = codeTextBox.SelectionStart;
            var currentChar = string.Empty;

            if (i > 0)
            {
                currentChar = codeTextBox.Text.Substring(i - 1, 1);
            }

            CheckKeyPressed(e, currentChar);
            _keyLastPressed = e.KeyData;
        }

        private void TextBoxTooltip_Enter(object sender, EventArgs e)
        {
            codeTextBox.Focus();
        }

        private void CheckKeyPressed(KeyEventArgs e, string currentChar)
        {
            switch (e.KeyCode)
            {
                case Keys.Back:
                    ActOnBackPressed(e, currentChar);
                    break;

                case Keys.Up:
                    ActOnUpPressed(e);
                    break;

                case Keys.Down:
                    ActOnDownPressed(e);
                    break;

                case Keys.D9:
                    ActOnD9Pressed();
                    break;

                case Keys.D8:
                    textBoxTooltip.Hide();
                    break;

                default:
                    if (_keyLastPressed == (Keys.ShiftKey | Keys.Shift))
                    {
                        ActOnUnderscorePressed(e);
                    }
                    else if (IsAlphanumeric(e))
                    {
                        ActOnAlphanumericKeyPressed(e);
                    }
                    else
                    {
                        ActOnNonAlphanumericKeyPressed(e);
                    }

                    break;
            }
        }

        private void ActOnUnderscorePressed(KeyEventArgs e)
        {
            var listBox = GetGListBox();

            if (e.KeyData == (Keys.OemMinus | Keys.Shift))
            {
                if (!listBox.Visible)
                {
                    if (PopulateGListBox())
                    {
                        var point = codeTextBox.GetPositionFromCharIndex(codeTextBox.SelectionStart);
                        point.Y += (int)Math.Ceiling(codeTextBox.Font.GetHeight()) + 2;
                        point.X += 2;

                        _statusBar1.Text = point.X + "," + point.Y;
                        listBox.Location = point;
                        listBox.BringToFront();
                        listBox.Show();
                    }
                }
            }
            else
            {
                listBox.Hide();
                _typed = string.Empty;
            }
        }

        private void ActOnBackPressed(KeyEventArgs e, string currentChar)
        {
            textBoxTooltip.Hide();

            if (_typed.Length > 0)
            {
                _typed = _typed.Substring(0, _typed.Length - 1);
            }

            if (currentChar == "_")
            {
                GetGListBox().Hide();
            }
        }

        private void ActOnUpPressed(KeyEventArgs e)
        {
            var listBox = GetGListBox();
            textBoxTooltip.Hide();

            if (listBox.Visible)
            {
                _wordMatched = true;
                if (listBox.SelectedIndex > 0)
                {
                    listBox.SelectedIndex--;
                }

                e.Handled = true;
            }
        }

        private void ActOnDownPressed(KeyEventArgs e)
        {
            var listBox = GetGListBox();
            textBoxTooltip.Hide();

            if (listBox.Visible)
            {
                _wordMatched = true;

                if (listBox.SelectedIndex < listBox.Items.Count - 1)
                {
                    listBox.SelectedIndex++;
                }

                e.Handled = true;
            }
        }

        private void ActOnD9Pressed()
        {
            var word = GetLastWord();
            _foundNode = false;
            _nameSpaceNode = null;
            _currentPath = string.Empty;

            SearchTree(treeViewItems.Nodes, word, true);

            if (_nameSpaceNode != null)
            {
                if (_nameSpaceNode.Tag is string)
                {
                    textBoxTooltip.Text = (string)_nameSpaceNode.Tag;

                    var point = codeTextBox.GetPositionFromCharIndex(codeTextBox.SelectionStart);
                    point.Y += (int)Math.Ceiling(codeTextBox.Font.GetHeight()) + 4/*2*/;
                    point.X -= 10;
                    textBoxTooltip.Location = point;
                    textBoxTooltip.Width = textBoxTooltip.Text.Length * 6;
                    textBoxTooltip.Size = new Size(textBoxTooltip.Text.Length * 6, textBoxTooltip.Height);

                    if (textBoxTooltip.Width > 300)
                    {
                        textBoxTooltip.Width = 300;
                        var height = 0;
                        height = textBoxTooltip.Text.Length / 50;
                        textBoxTooltip.Height = height * 15;
                    }

                    textBoxTooltip.Show();
                }
            }
        }

        private void ActOnNonAlphanumericKeyPressed(KeyEventArgs e)
        {
            var listBox = GetGListBox();

            if (listBox.Visible)
            {
                if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Space)
                {
                    textBoxTooltip.Hide();
                    SelectItem();
                    _typed = string.Empty;
                    _wordMatched = false;
                    e.Handled = true;
                }

                listBox.Hide();
            }
        }

        private void ActOnAlphanumericKeyPressed(KeyEventArgs e)
        {
            var listBox = GetGListBox();

            if (listBox.Visible)
            {
                _typed += (char)e.KeyValue;
                _wordMatched = false;

                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    if (listBox.Items[i].ToString().ToLower().StartsWith(_typed.ToLower()))
                    {
                        _wordMatched = true;
                        GetGListBox().SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                _typed = string.Empty;
            }
        }

        private bool IsAlphanumeric(KeyEventArgs e)
        {
            if (e.KeyValue < 48
                || (e.KeyValue >= 58 && e.KeyValue <= 64)
                || (e.KeyValue >= 91 && e.KeyValue <= 96)
                || e.KeyValue > 122)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region GListBox
        private void AddGListBox(TabControl tabControl, int pageIndex)
        {
            var gListBox = new GListBox()
            {
                Name = "GListBox",
                BorderStyle = BorderStyle.FixedSingle,
                DrawMode = DrawMode.OwnerDrawFixed,
                ImageList = imageList1,
                Location = new Point(136, 300),
                Size = new Size(208, 60),
                Visible = false,
                IntegralHeight = false,
                ItemHeight = 17,
            };

            gListBox.SelectedIndexChanged += new EventHandler(GListBox_SelectedIndexChanged);
            gListBox.DoubleClick += new EventHandler(GListBox_DoubleClick);
            gListBox.KeyDown += new KeyEventHandler(GListBox_KeyDown);
            //tabControl.TabPages[pageIndex].Controls.Add(gListBox);
            tabControl.TabPages[0].Controls.Add(gListBox);
        }

        private GListBox GetGListBox()
        {
            return textEditorTabControl.SelectedTab.Controls.Find("GListBox", true)
                                                                                              .First() as GListBox;
        }

        private void GListBox_DoubleClick(object sender, EventArgs e)
        {
            var listBoxAutoComplete = GetGListBox();

            if (listBoxAutoComplete.SelectedItems.Count == 1)
            {
                _wordMatched = true;
                SelectItem();
                listBoxAutoComplete.Hide();
                codeTextBox.Focus();
                _wordMatched = false;
            }
        }

        private void GListBox_KeyDown(object sender, KeyEventArgs e)
        {
            codeTextBox.Focus();
        }

        private void GListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            codeTextBox.Focus();
        }

        private bool PopulateGListBox()
        {
            var result = false;
            var word = GetLastWord();

            if (word != string.Empty)
            {
                _findNodeResult = null;
                FindNode(word, treeViewItems.Nodes);

                if (_findNodeResult != null)
                {
                    GetGListBox().Items.Clear();

                    if (_findNodeResult.Nodes.Count > 0)
                    {
                        result = true;

                        var items = new MemberItem[_findNodeResult.Nodes.Count];

                        for (int i = 0; i < _findNodeResult.Nodes.Count; i++)
                        {
                            var memberItem = new MemberItem()
                            {
                                _displayText = _findNodeResult.Nodes[i].Text,
                                _tag = _findNodeResult.Nodes[i].Tag
                            };

                            items[i] = memberItem;
                        }

                        Array.Sort(items);

                        DisplayListBoxItemIcons(items);
                    }
                }
            }

            return result;
        }
        #endregion

        private void InitializeListViews()
        {
            var tables = _simpleTablesInstance._tables;
            tableListView.View = View.List;

            foreach (var table in tables)
            {
                tableListView.Items.Add(table.Key.Capitalize(), table.Value);
            }

            var nominals = _simpleTablesInstance._nominalTable;
            nominalListView.View = View.List;

            foreach (var nominal in nominals)
            {
                nominalListView.Items.Add(nominal.Key.Capitalize(), nominal.Value);
            }
        }

        private void EnableDragDropEvents()
        {
            tableListView.ItemDrag += new ItemDragEventHandler(TableListView_ItemDrag);
            nominalListView.ItemDrag += new ItemDragEventHandler(NominalListView_ItemDrag);
            codeTextBox.DragEnter += new DragEventHandler(CodeTextBox_DragEnter);
            codeTextBox.DragDrop += new DragEventHandler(CodeTextBox_DragDrop);
            codeTextBox.AllowDrop = true;
        }

        private void TableListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            tableListView.DoDragDrop(tableListView.SelectedItems, DragDropEffects.Move);
        }

        private void NominalListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            nominalListView.DoDragDrop(nominalListView.SelectedItems, DragDropEffects.Move);
        }

        private void CodeTextBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void CodeTextBox_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection).ToString(), false))
            {
                var selectedListViewItems
                    = (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection));

                foreach (ListViewItem listItem in selectedListViewItems)
                {
                    var index = codeTextBox.GetCharIndexFromPosition(codeTextBox.PointToClient(Cursor.Position));
                    codeTextBox.SelectionStart = index;
                    codeTextBox.SelectionLength = 0;
                    codeTextBox.SelectedText = listItem.ImageKey;
                }
            }
        }

        private void UpdateEditorTabText(string fileName)
        {
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            textEditorTabControl.TabPages[0].Text = filenameWithoutExtension;
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            LoadFile();
        }

        private void LoadFile()
        {
            var openFileDialog = new OpenFileDialog()
            {
                RestoreDirectory = true
            };

            if (_changedSinceLastSave)
            {
                var saveChanges = MessageBox.Show(
                            "You have unsaved changes. Do you want to save them first?",
                            "Confirm",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                if ((saveChanges == DialogResult.No)
                            || ((saveChanges == DialogResult.Yes) && SaveFile(_openFilePath)))
                {
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ClearCodeTextBox();
                        ReadFile(openFileDialog);
                    }
                }
            }
            else if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ClearCodeTextBox();
                ReadFile(openFileDialog);
            }

            // NEED TO TAKE INTO ACCOUNT IF TAB HAS BEEN CLOSED
        }

        private void ReadFile(OpenFileDialog openFileDialog)
        {
            using (var streamReader = new StreamReader(openFileDialog.OpenFile()))
            {
                try
                {
                    var line = string.Empty;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        codeTextBox.AppendText(line + "\n");
                    }

                    _openFilePath = openFileDialog.FileName;
                    _changedSinceLastSave = false;
                    UpdateEditorTabText(openFileDialog.FileName);
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

        private void ClearCodeTextBox()
        {
            codeTextBox.Text = string.Empty;
        }

        private void SaveAsButton_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }

        private bool SaveFileAs()
        {
            var saveFileDialog = new SaveFileDialog()
            {
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                return SaveFile(saveFileDialog.FileName);
            }
            else
            {
                return false;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveFile(_openFilePath);
        }

        private bool SaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return SaveFileAs();
            }

            using (var streamWriter = new StreamWriter(fileName))
            {
                try
                {
                    streamWriter.Write(codeTextBox.Text);
                    _changedSinceLastSave = false;
                    _openFilePath = fileName;
                    UpdateEditorTabText(fileName);
                    MessageBox.Show("Saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void NewTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenTab(textEditorTabControl, "<New tab>");
        }

        private void OpenTab(TabControl tabControl, string fileName)
        {
            var tabPage = new TabPage(fileName);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;
            tabControl.DrawItem += new DrawItemEventHandler(TextEditorTabControl_DrawItem);
            tabControl.MouseDown += new MouseEventHandler(TextEditorTabControl_MouseDown);
            SetUpTab(tabControl, tabControl.TabPages.Count - 1);
        }

        private void CloseTab(TabControl tabControl, int i)
        {
            tabControl.TabPages.RemoveAt(i); // Ask if sure want to close?
            _openFilePath = string.Empty;
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadFile();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(_openFilePath);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }

        private RichTextBox GetTabTextBox(TabControl tabControl)
        {
            var pageIndex = tabControl.SelectedIndex;
            return tabControl.SelectedTab.Controls.Find("richTextBox" + pageIndex, true)
                                                                             .First() as RichTextBox;
        }

        #region Working Intellisense
        private void ReadAssembly()
        {
            treeViewItems.Nodes.Clear();

            try
            {
                _assembly = Assembly.Load("Intellisense");
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not load the assembly.\nException message: " + e.Message);
                return;
            }

            var assemblyTypes = _assembly.GetTypes();
            _dictionaries = new Hashtable();
            //_namespaces = new Hashtable();

            foreach (var type in assemblyTypes)
            {
                if (type.IsClass)
                {
                    AddClassDictionariesToTree(type);
                }

                //AddNamespacesToTree(type);
            }

            DebugCallRecursive();
        }

        private void AddClassDictionariesToTree(Type type)
        {
            var instance = Activator.CreateInstance(type);

            var instanceVariables = instance.GetType()
                                                                 .GetFields()
                                                                 .Select(field => field.GetValue(instance))
                                                                 .ToList();

            foreach (var variable in instanceVariables)
            {
                var variableType = variable.GetType();
                var isDictionary =
                    variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(Dictionary<,>);

                if (isDictionary)
                {
                    var dictionary = (Dictionary<string, string>)null;

                    try
                    {
                        dictionary = (Dictionary<string, string>)variable;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Could not build the hashtable required.\nException message: " + e.Message);
                        return;
                    }

                    ProcessDictionaryItems(dictionary, type);
                }
            }
        }

        private void ProcessDictionaryItems(Dictionary<string, string> dictionary, Type type)
        {
            foreach (var entry in dictionary)
            {
                var value = entry.Value;

                if (_dictionaries.ContainsKey(value))
                {
                    var treeNode = (TreeNode)_dictionaries[value];
                    treeNode = treeNode.Nodes.Add(value);
                }
                else
                {
                    var membersNode = (TreeNode)null;

                    if (value.IndexOf("_") != -1)
                    {
                        _nameSpaceNode = null;
                        _foundNode = false;
                        _currentPath = string.Empty;
                        SearchTree(treeViewItems.Nodes, value, false);

                        if (_nameSpaceNode == null)
                        {
                            membersNode = CreateNewPathEntry(membersNode, value);
                            // treeViewItems.Nodes.Add(value);
                        }
                        else
                        {
                            membersNode = AddToExistingPathEntry(membersNode, value, type);
                            treeViewItems.Nodes.Add(value);
                        }
                    }
                    else
                    {
                        membersNode = treeViewItems.Nodes.Add(value);
                    }
                }
            }
        }

        private TreeNode CreateNewPathEntry(TreeNode membersNode, string value)
        {
            var parts = value.Split('_');
            var treeNode = treeViewItems.Nodes.Add(parts[0]);
            var sValue = parts[0];

            if (!_dictionaries.ContainsKey(sValue))
            {
                _dictionaries.Add(sValue, treeNode);
            }

            for (int i = 1; i < parts.Length; i++)
            {
                treeNode = treeNode.Nodes.Add(parts[i]);
                sValue += "_" + parts[i];

                if (!_dictionaries.ContainsKey(sValue))
                {
                    _dictionaries.Add(sValue, treeNode);
                }
            }

            return membersNode = treeNode.Nodes.Add(value); // ?? Should it be split?
        }

        private TreeNode AddToExistingPathEntry(TreeNode membersNode, string value, Type type)
        {
            var parts = value.Split('_');
            var newDictionaryNode = (TreeNode)null;
            newDictionaryNode = _nameSpaceNode.Nodes.Add(parts[parts.Length - 1]);
            _dictionaries.Add(value, newDictionaryNode);

            if (newDictionaryNode != null)
            {
                membersNode = newDictionaryNode.Nodes.Add(value); // Should it be first word?

                if (type.IsClass) // Remove? Don't know if needed
                {
                    membersNode.Tag = MemberTypes.Custom;
                }
            }

            return membersNode;
        }

        /*private void AddNamespacesToTree(Type type)
        {
            // NEED TO GO THROUGH
            if (type.Namespace != null)
            {
                if (_namespaces.ContainsKey(type.Namespace))
                {
                    // Already got namespace, add the class to it
                    TreeNode treeNode = (TreeNode)_namespaces[type.Namespace];
                    treeNode = treeNode.Nodes.Add(type.Name);

                    if (type.IsClass)
                    {
                        treeNode.Tag = MemberTypes.Custom;
                    }
                }
                else
                {
                    // New namespace
                    TreeNode membersNode = null;

                    if (type.Namespace.IndexOf(".") != -1)
                    {
                        // Search for already existing parts of the namespace
                        _nameSpaceNode = null;
                        _foundNode = false;

                        _currentPath = string.Empty;
                        SearchTree(this.treeViewItems.Nodes, type.Namespace, false);

                        // No existing namespace found
                        if (_nameSpaceNode == null)
                        {
                            // Add the namespace
                            string[] parts = type.Namespace.Split('.');

                            TreeNode treeNode = treeViewItems.Nodes.Add(parts[0]);
                            string sNamespace = parts[0];

                            if (_!namespaces.ContainsKey(sNamespace))
                            {
                                _namespaces.Add(sNamespace, treeNode);
                            }

                            for (int i = 1; i < parts.Length; i++)
                            {
                                treeNode = treeNode.Nodes.Add(parts[i]);
                                sNamespace += "." + parts[i];
                                if (!_namespaces.ContainsKey(sNamespace))
                                {
                                    _namespaces.Add(sNamespace, treeNode);
                                }
                            }

                            membersNode = treeNode.Nodes.Add(type.Name);
                        }
                        else
                        {
                            // Existing namespace, add this namespace to it,
                            // and add the class
                            string[] parts = type.Namespace.Split('.');
                            TreeNode newNamespaceNode = null;

                            if (!_namespaces.ContainsKey(type.Namespace))
                            {
                                newNamespaceNode = _nameSpaceNode.Nodes.Add(parts[parts.Length - 1]);
                                _namespaces.Add(type.Namespace, newNamespaceNode);
                            }
                            else
                            {
                                newNamespaceNode = (TreeNode)_namespaces[type.Namespace];
                            }

                            if (newNamespaceNode != null)
                            {
                                membersNode = newNamespaceNode.Nodes.Add(type.Name);
                                if (type.IsClass)
                                {
                                    membersNode.Tag = MemberTypes.Custom;
                                }
                            }
                        }

                    }
                    else
                    {
                        // Single root namespace, add to root
                        membersNode = treeViewItems.Nodes.Add(type.Namespace);
                    }
                }
            }
        }*/

        private void SearchTree(TreeNodeCollection treeNodes, string path, bool continueUntilFind)
        {
            // CUT METHOD DOWN
            if (_foundNode)
            {
                return;
            }

            var subpath = string.Empty;
            var lastIndexOfUnderscore = path.IndexOf("_");

            if (lastIndexOfUnderscore != -1)
            {
                subpath = path.Substring(0, lastIndexOfUnderscore);

                if (_currentPath != string.Empty)
                {
                    _currentPath += "_" + subpath;
                }
                else
                {
                    _currentPath = subpath;
                }

                // Knock off the first part
                path = path.Remove(0, lastIndexOfUnderscore + 1);
            }
            else
            {
                _currentPath += "_" + path;
            }

            for (int i = 0; i < treeNodes.Count; i++)
            {
                if (treeNodes[i].FullPath == _currentPath)
                {
                    if (continueUntilFind)
                    {
                        _nameSpaceNode = treeNodes[i];
                    }

                    _nameSpaceNode = treeNodes[i];

                    // got an underscore, continue, or return
                    SearchTree(treeNodes[i].Nodes, path, continueUntilFind);
                }
                else if (!continueUntilFind)
                {
                    _foundNode = true;
                    return;
                }
            }
        }

        private void FindNode(string path, TreeNodeCollection treeNodes)
        {
            for (int i = 0; i < treeNodes.Count; i++)
            {
                if (treeNodes[i].FullPath == path)
                {
                    _findNodeResult = treeNodes[i];
                    break;
                }
                else if (treeNodes[i].Nodes.Count > 0)
                {
                    FindNode(path, treeNodes[i].Nodes);
                }
            }
        }

        private void DisplayListBoxItemIcons(MemberItem[] items)
        {
            for (int n = 0; n < items.Length; n++)
            {
                var imageindex = 0;

                if (items[n]._tag != null)
                {
                    imageindex = 2;
                    if (items[n]._tag is MemberTypes memberType)
                    {
                        switch (memberType)
                        {
                            case MemberTypes.Custom:
                                imageindex = 1;
                                break;
                            case MemberTypes.Property:
                                imageindex = 3;
                                break;
                            case MemberTypes.Event:
                                imageindex = 4;
                                break;
                        }
                    }
                }

                GetGListBox().Items.Add(new GListBoxItem(items[n]._displayText, imageindex));
            }
        }

        private void SelectItem()
        {
            if (_wordMatched)
            {
                var prefixend = codeTextBox.SelectionStart - _typed.Length;
                var suffixstart = codeTextBox.SelectionStart + _typed.Length;

                if (suffixstart >= codeTextBox.Text.Length)
                {
                    suffixstart = codeTextBox.Text.Length;
                }

                var prefix = codeTextBox.Text.Substring(0, prefixend);
                var fill = GetGListBox().SelectedItem.ToString();
                var suffix = codeTextBox.Text.Substring(suffixstart, codeTextBox.Text.Length - suffixstart);

                codeTextBox.Text = prefix + fill + suffix;
                codeTextBox.SelectionStart = prefix.Length + fill.Length;
            }
        }

        private string GetLastWord()
        {
            var word = string.Empty;
            var position = codeTextBox.SelectionStart;

            if (position > 1)
            {
                var substring = string.Empty;
                var currentChar = default(char);

                while (currentChar != ' ' && currentChar != 10 && position > 0)
                {
                    position--;
                    substring = codeTextBox.Text.Substring(position, 1);
                    currentChar = (char)substring[0];
                    word += currentChar;
                }

                var charArray = word.ToCharArray();
                Array.Reverse(charArray);
                word = new string(charArray);
            }

            return word.Trim();
        }

        #region testing
        private int index = 0;

        private string[] DebugPrintTreeView(TreeNode treeNode, string[] treeArray, int index)
        {
            treeArray[index] = treeNode.Text;
            index++;

            foreach (var tn in treeNode.Nodes)
            {
                treeArray = DebugPrintTreeView((TreeNode)tn, treeArray, index);
            }

            return treeArray;
        }

        private void DebugCallRecursive()
        {
            var nodes = treeViewItems.Nodes;
            var treeArray = new string[nodes.Count * 2];

            for (int i = 0; i < nodes.Count; i++)
            {
                treeArray = DebugPrintTreeView(nodes[i], treeArray, index);

                foreach (var item in treeArray)
                {
                    Debug.Write(item + "\n");
                }

                Debug.Write("\n========\n");
            }
        }
        #endregion

        #endregion

        #region Intellisense
        //private GListBox GetGListBox(TabControl tabControl)
        //{
        //    var pageIndex = tabControl.SelectedIndex;
        //    return tabControl.SelectedTab.Controls.Find("GListBox" + pageIndex, true)
        //                                                                     .First() as GListBox;
        //}

        //private void LoadAssembly() // Rename? Just loads dictionary into tree view hierarchy
        //{
        //    _dictionaries = new Hashtable(); // Rename namespaces?
        //    treeViewItems.Nodes.Clear();

        //    try
        //    {
        //        _assembly = Assembly.Load("Intellisense");
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show("Could not load the assembly file.\nException message: " + e.Message);
        //        return;
        //    }

        //    var assemblyTypes = _assembly.GetTypes();
        //    _dictionaries = new Hashtable();
        //    //namespaces = new Hashtable();

        //    foreach (var type in assemblyTypes)
        //    {
        //        if (type.IsClass)
        //        {
        //            AddClassDictionariesToTree(type);
        //        }

        //        //AddNamespacesToTree(type);
        //    }

        //    DebugCallRecursive();
        //}

        //private void AddClassDictionariesToTree(Type type)
        //{
        //    var instance = Activator.CreateInstance(type);

        //    var instanceVariables = instance.GetType()
        //                                                         .GetFields()
        //                                                         .Select(field => field.GetValue(instance))
        //                                                         .ToList();

        //    foreach (var variable in instanceVariables)
        //    {
        //        var variableType = variable.GetType();
        //        var isDictionary =
        //            variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(Dictionary<,>);

        //        if (isDictionary)
        //        {
        //            var dictionary = (Dictionary<string, string>)null;

        //            try
        //            {
        //                dictionary = (Dictionary<string, string>)variable;
        //            }
        //            catch (Exception e)
        //            {
        //                MessageBox.Show("Could not build the hashtable required.\nException message: " + e.Message);
        //                return;
        //            }

        //            ProcessDictionaryItems(dictionary, type);
        //        }
        //    }
        //}

        //private int index = 0;

        //private string[] DebugPrintTreeView(TreeNode treeNode, string[] treeArray, int index)
        //{
        //    treeArray[index] = treeNode.Text;
        //    index++;

        //    foreach (var tn in treeNode.Nodes)
        //    {
        //        treeArray = DebugPrintTreeView((TreeNode)tn, treeArray, index);
        //    }

        //    return treeArray;
        //}

        //private void DebugCallRecursive()
        //{
        //    var treeArray = new string[20];
        //    var nodes = treeViewItems.Nodes;

        //    foreach (var n in nodes)
        //    {
        //        treeArray = DebugPrintTreeView((TreeNode)n, treeArray, index);
        //    }

        //    var printString = string.Empty;
        //    foreach (var item in treeArray)
        //    {
        //        printString += "\n" + item;
        //    }

        //    MessageBox.Show(printString);
        //}

        //private void AddMembers(TreeNode treeNode, Type type)
        //{
        //    // Get all members except methods
        //    var memberInfo = type.GetMembers();

        //    for (int j = 0; j < memberInfo.Length; j++)
        //    {
        //        if (memberInfo[j].ReflectedType.IsPublic 
        //            && memberInfo[j].MemberType != MemberTypes.Method)
        //        {
        //            var node = treeNode.Nodes.Add(memberInfo[j].Name);
        //            node.Tag = memberInfo[j].MemberType;
        //        }
        //    }

        //    // Get all methods
        //    var methodInfo = type.GetMethods();

        //    for (int j = 0; j < methodInfo.Length; j++)
        //    {
        //        var node = treeNode.Nodes.Add(methodInfo[j].Name);
        //        string parms = string.Empty;

        //        var parameterInfo = methodInfo[j].GetParameters();

        //        for (int f = 0; f < parameterInfo.Length; f++)
        //        {
        //            parms += parameterInfo[f].ParameterType.ToString() + " " + parameterInfo[f].Name + ", ";
        //        }

        //        // Knock off remaining ", "
        //        if (parms.Length > 2)
        //        {
        //            parms = parms.Substring(0, parms.Length - 2);
        //        }

        //        node.Tag = parms;
        //    }
        //}
        #endregion

        private void CodeTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!_changedSinceLastSave)
            {
                textEditorTabControl.TabPages[0].Text += "*";
                _changedSinceLastSave = true;
            }
        }
    }
}
