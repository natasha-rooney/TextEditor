using System;
using System.Windows.Forms;

namespace ComplianceEditor
{
    public partial class EditorForm : Form
    {

        private void OpenButton_Click(object sender, EventArgs e)
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
                    var pageIndex = Array.IndexOf(_openTabFilePaths, fileName);
                    textEditorTabControl.SelectedIndex = pageIndex;
                }
                else
                {
                    OpenTab(textEditorTabControl, fileName);
                    ReadFile(openFileDialog);
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

                    _tabsChangedSinceSave[textEditorTabControl.SelectedIndex] = false; // Updating bc changing text triggers text changed event

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
            else
            {
                var selectedIndex = textEditorTabControl.SelectedIndex;
                if (!_tabsChangedSinceSave[selectedIndex])
                {
                    return false;
                }

                    using (var streamWriter = new StreamWriter(fileName))
                    {
                    try
                    {
                        streamWriter.Write(GetTabTextBox(textEditorTabControl).Text);

                        var pageIndex = textEditorTabControl.SelectedIndex;
                        _tabsChangedSinceSave[pageIndex] = false;
                        _openTabFilePaths[pageIndex] = fileName;
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
                _openTabFilePaths[tabControl.SelectedIndex] = fileName;
                _tabsChangedSinceSave[tabControl.SelectedIndex] = false;
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

        private void CloseTab(TabControl tabControl)
        {
            var closingTabPageIndex = tabControl.SelectedIndex;

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

            tabControl.TabPages.RemoveAt(closingTabPageIndex);
            _openTabFilePaths[closingTabPageIndex] = null;
            _tabsChangedSinceSave[closingTabPageIndex] = false;

            for (int i = closingTabPageIndex; i < tabControl.TabPages.Count; i++)
            {
                _openTabFilePaths[i] = _openTabFilePaths[i + 1];
                _tabsChangedSinceSave[i] = _tabsChangedSinceSave[i + 1];
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(_openTabFilePaths[textEditorTabControl.SelectedIndex]);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }

    }
}
