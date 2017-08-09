using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Compliance.Intellisense;
using System.Linq;
using System.Collections.Generic;

namespace Compliance.Editor
{
    public partial class EditorForm : Form
    {
        private int _closeRecDistanceFromLeft;
        private int _maxNumTabs = 10;
        private int _tabItemsDistanceFromTop;
        //private string _openFilePath = string.Empty;
        private RichTextBox _tempTextBox;
        private TabPage _tempTabPage;
        private Simple_Tables _simpleTablesInstance = new Simple_Tables();

        private bool[] _tabsChangedSinceSave;
        private string[] _openTabFilePaths;

        //private bool _foundNode = false;
        //private bool _wordMatched = false;
        //private string _currentPath;
        //private Assembly _assembly;
        //private Hashtable _dictionaries;
        //private TreeNode _findNodeResult = null;
        //private TreeNode _nameSpaceNode;

        public EditorForm()
        {
            _tabsChangedSinceSave = new bool[_maxNumTabs];
            _openTabFilePaths = new string[_maxNumTabs];
            _tabsChangedSinceSave.Initialize();
            _openTabFilePaths.Initialize();

            InitializeComponent();
            InitializeTabs();
            InitializeListViews();
            EnableListDragDropEvents();
            //LoadAssembly();
        }

        private void InitializeTabs()
        {
            textEditorTabControl.TabPages[0].Text = "<New File>";
            textEditorTabControl.DrawItem += new DrawItemEventHandler(TextBoxTabControl_DrawItem);
            textEditorTabControl.MouseDown += new MouseEventHandler(TextBoxTabControl_MouseDown);
            AddRichTextBox(textEditorTabControl, 0);

            toolsTabControl.TabPages[0].Text = "Tables";
            toolsTabControl.TabPages[1].Text = "Nominal";
        }

        private void AddRichTextBox(TabControl tabControl, int pageIndex)
        {
            var richTextBox = new RichTextBox()
            {
                Name = "richTextBox" + pageIndex,
                Location = new Point(5, 5),
                Size = new Size(
                ClientSize.Width - 40,
                ClientSize.Height - 100),

                Anchor =
                    AnchorStyles.Top
                    | AnchorStyles.Bottom
                    | AnchorStyles.Left
                    | AnchorStyles.Right
            };

            richTextBox = EnableTextBoxDragDropEvents(tabControl, richTextBox);
            tabControl.TabPages[pageIndex].Controls.Add(richTextBox);

            //richTextBox.KeyDown += new KeyEventHandler(RichTextBox_KeyDown);
            //richTextBox.MouseDown += new MouseEventHandler(RichTextBox_MouseDown);
        }

        private void TextBoxTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            _closeRecDistanceFromLeft = e.Bounds.Right - 18;
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
                e.Bounds.Left + 4,
                _tabItemsDistanceFromTop);

            textEditorTabControl.Padding = new Point(15, 5);

            e.DrawFocusRectangle();
        }

        private void TextBoxTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < textEditorTabControl.TabCount; i++)
            {
                var tabRecCloseButton = textEditorTabControl.GetTabRect(i);
                tabRecCloseButton.Offset(_closeRecDistanceFromLeft, _tabItemsDistanceFromTop);
                tabRecCloseButton.Width = 5;
                tabRecCloseButton.Height = 10;

                if (tabRecCloseButton.Contains(e.Location))
                {
                    // Problem - closing all tabs.
                    _tempTabPage = textEditorTabControl.TabPages[i];
                    CloseTab(textEditorTabControl, i);
                }
            }
        }

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

        private void EnableListDragDropEvents()
        {
            tableListView.ItemDrag += new ItemDragEventHandler(TableListView_ItemDrag);
            nominalListView.ItemDrag += new ItemDragEventHandler(NominalListView_ItemDrag);
        }

        private RichTextBox EnableTextBoxDragDropEvents(TabControl tabControl, RichTextBox richTextBox)
        {
            _tempTextBox = richTextBox;
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
            if (_tempTextBox == null)
            {
                MessageBox.Show("Could not enable drag and drop for a textbox. Error: No text box has been provided.");
                return;
            }

            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection).ToString(), false))
            {
                var selectedListViewItems
                    = (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection));

                foreach (ListViewItem listItem in selectedListViewItems)
                {
                    var index = _tempTextBox.GetCharIndexFromPosition(_tempTextBox.PointToClient(Cursor.Position));
                    _tempTextBox.SelectionStart = index;
                    _tempTextBox.SelectionLength = 0;
                    _tempTextBox.SelectedText = listItem.ImageKey;
                }
            }
        }

        private void UpdateTextEditorTabText(string fileName)
        {
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            textEditorTabControl.TabPages[textEditorTabControl.SelectedIndex].Text = filenameWithoutExtension;
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

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenTab(textEditorTabControl, openFileDialog.FileName);
                ClearCodeTextBox();
                ReadFile(openFileDialog);
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

                    var pageIndex = textEditorTabControl.SelectedIndex;
                    _openTabFilePaths[pageIndex] = openFileDialog.FileName;
                    _tabsChangedSinceSave[pageIndex] = false;
                    UpdateTextEditorTabText(openFileDialog.FileName);
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
            GetTabTextBox(textEditorTabControl).Text = string.Empty;
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
            SaveFile(_openTabFilePaths[textEditorTabControl.SelectedIndex]);
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
                    streamWriter.Write(GetTabTextBox(textEditorTabControl).Text);
                    //_changedSinceLastSave = false;
                    //_openFilePath = fileName;
                    _openTabFilePaths[textEditorTabControl.SelectedIndex] = fileName;
                    UpdateTextEditorTabText(fileName);
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
            OpenTab(textEditorTabControl, "<New File>");
        }

        private void OpenTab(TabControl tabControl, string fileName)
        {
            if (tabControl.TabPages.Count < _maxNumTabs)
            {
                var tabPage = new TabPage(fileName)
                {
                    BackColor = Color.White
                };

                tabControl.TabPages.Add(tabPage);
                tabControl.SelectedTab = tabPage;
                tabControl.DrawItem += new DrawItemEventHandler(TextBoxTabControl_DrawItem);
                tabControl.MouseDown += new MouseEventHandler(TextBoxTabControl_MouseDown);
                AddRichTextBox(tabControl, tabControl.TabPages.Count - 1);
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

        private void CloseTab(TabControl tabControl, int i)
        {
            var closingTabPageIndex = tabControl.TabPages.IndexOf(_tempTabPage);

            if (_tabsChangedSinceSave[closingTabPageIndex])
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

            tabControl.TabPages.RemoveAt(i);
            _openTabFilePaths[closingTabPageIndex] = null;
            _tabsChangedSinceSave[closingTabPageIndex] = false;
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadFile();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(_openTabFilePaths[textEditorTabControl.SelectedIndex]);
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

        #region Intellisense
        //private void SearchTree(TreeNodeCollection treeNodes, string path, bool continueUntilFind)
        //{
        //    if (_foundNode)
        //    {
        //        return;
        //    }

        //    var p = string.Empty;
        //    var n = 0;
        //    n = path.IndexOf(".");

        //    if (n != -1)
        //    {
        //        p = path.Substring(0, n);

        //        if (_currentPath != string.Empty)
        //        {
        //            _currentPath += "." + p;
        //        }
        //        else
        //        {
        //            _currentPath = p;
        //        }

        //        // Knock off the first part
        //        path = path.Remove(0, n + 1);
        //    }
        //    else
        //    {
        //        _currentPath += "." + path;
        //    }

        //    for (int i = 0; i < treeNodes.Count; i++)
        //    {
        //        if (treeNodes[i].FullPath == _currentPath)
        //        {
        //            if (continueUntilFind)
        //            {
        //                _nameSpaceNode = treeNodes[i];
        //            }

        //            _nameSpaceNode = treeNodes[i];

        //            // got a dot, continue, or return
        //            SearchTree(treeNodes[i].Nodes, path, continueUntilFind);

        //        }
        //        else if (!continueUntilFind)
        //        {
        //            _foundNode = true;
        //            return;
        //        }
        //    }
        //}
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

        // Feature for later
        //private void CodeTextBox_TextChanged(object sender, EventArgs e)
        //{
        //    if (!_changedSinceLastSave)
        //    {
        //        textEditorTabControl.TabPages[0].Text += "*";
        //        _changedSinceLastSave = true;
        //    }
        //}
    }
}
