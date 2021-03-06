﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NLog;
using warnings.components;
using warnings.components.ui;
using warnings.conditions;
using warnings.refactoring;
using warnings.util;

namespace warnings.ui
{
    public partial class RefactoringWariningsForm : Form
    {
        /* Saving all the message and listview item pairs currently showing on the form. */
        private readonly List<IRefactoringWarningMessage> messagesInListView;

        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof(RefactoringWariningsForm));

        public RefactoringWariningsForm()
        {
            InitializeComponent();
            messagesInListView = new List<IRefactoringWarningMessage>();
        }
     
        /// <summary>
        /// This is the control part of the control-view-model pattern.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRemoveWarningButtonClick(object sender, EventArgs e)
        {
            var toRemoveCodeIssueComputers = new List<ICodeIssueComputer>();
            var items = refactoringWarningsListView.SelectedItems;
            
            // For each item in the list of selected items.
            foreach (ListViewItem item in items)
            {
                // Get the index of this item among all the items.
                int index = refactoringWarningsListView.Items.IndexOf(item);

                // Get the corresponding code issue computer to this item.
                var computer = messagesInListView.ElementAt(index).CodeIssueComputer;

                // Add the computer to the toremove list, if it is not aready there. 
                if(!toRemoveCodeIssueComputers.Contains(computer))
                {
                    toRemoveCodeIssueComputers.Add(computer);
                }
            }

            // If the toRemove list has some element, remove these computers directly from the 
            // code issue component. 
            if(toRemoveCodeIssueComputers.Any())
            {
                GhostFactorComponents.RefactoringCodeIssueComputerComponent.
                    RemoveCodeIssueComputers(toRemoveCodeIssueComputers);
            }
        }

 
       
        private ListViewItem CreateListViewItem(IEnumerable<string> messages, int imageIndex)
        {
            // Get the enumerator.
            var enumerator = messages.GetEnumerator();

            // If messagesInListView have the first item
            if (enumerator.MoveNext())
            {
                // Create an item leaded by the first element.
                var item = new ListViewItem(enumerator.Current);

                // Add subitems by reading the rest element. 
                for (; enumerator.MoveNext(); )
                {
                    item.SubItems.Add(enumerator.Current);
                }
                item.ImageIndex = imageIndex;
                return item;
            }
            return null;
        }


        public void AddRefactoringWarnings(IEnumerable<IRefactoringWarningMessage> messages)
        {
            foreach (var message in messages)
            {
                logger.Info(message.ToString());
                AddRefactoringWarning(message);
            }
        }


        /* Split a IRefactoringWarningMessage to string elements. */
        private IEnumerable<string> Split2MessageElements(IRefactoringWarningMessage message)
        {
            var messageElements = new List<string>();
            messageElements.Add(message.File);
            messageElements.Add(message.Line.ToString());
            var typeName = RefactoringTypeUtil.GetRefactoringTypeName(message.RefactoringType);

            messageElements.Add(typeName);
            messageElements.Add(message.Description);
            return messageElements;
        }

        public bool AddRefactoringWarning(IRefactoringWarningMessage message)
        {
            // Create a list view item by the given messagesInListView.
            var item = CreateListViewItem(Split2MessageElements(message), 0);

            // If the item is created, add to the list view.
            if (item != null)
            {
                refactoringWarningsListView.Items.Add(item);
                
                // Save message.
                messagesInListView.Add(message);
                refactoringWarningsListView.Invalidate();
                logger.Info("Item added.");
                return true;
            }
            return false;
        }

        /* Invoked when double clicking a warning, shall redirect to where the problem is. */
        private void listView1_DoubleClicked(object sender, EventArgs e)
        {
            var selectedItems = refactoringWarningsListView.SelectedItems;
            if(selectedItems.Count > 0)
            {
                var removedCodeIssueComputers = new List<ICodeIssueComputer>();
                foreach (ListViewItem item in selectedItems)
                {
                    int index = refactoringWarningsListView.Items.IndexOf(item);
                    var message = messagesInListView.ElementAt(index);
                    removedCodeIssueComputers.Add(message.CodeIssueComputer);
                }
                GhostFactorComponents.RefactoringCodeIssueComputerComponent.
                    RemoveCodeIssueComputers(removedCodeIssueComputers.Distinct());
            }
        }

        public void RemoveRefactoringWarnings(Predicate<IRefactoringWarningMessage> removingMessagesConditions)
        {
            var indexes = new List<int>();
          
            // For all the messages currently in the list.
            foreach (var inListMessage in messagesInListView)
            {
                // If the current message met with the given removing message condition.
                // Add the index of this message to indexes.
                if(removingMessagesConditions.Invoke(inListMessage))
                {
                    indexes.Add(messagesInListView.IndexOf(inListMessage));
                }
            }

            // Remove all messages as well as item in the list view.
            foreach (int i in indexes)
            {
                messagesInListView.RemoveAt(i);
                refactoringWarningsListView.Items.RemoveAt(i);
            }
            refactoringWarningsListView.Invalidate();
        }

        /// <summary>
        /// Set the text label indicates how many problematic refactorings are there.
        /// </summary>
        /// <param name="count"></param>
        public void SetProblematicRefactoringsCount(int count)
        {
            refactoringCountLabel.Text = count.ToString();
        }

        /// <summary>
        /// Set the current active document name.
        /// </summary>
        /// <param name="info"></param>
        public void SetActiveDocumentText(string info)
        {
            ActiveSourceFile.Text = info;
        }

        /// <summary>
        /// Set the shown supported refactoring types.
        /// </summary>
        /// <param name="types"></param>
        public void SetSupportedRefactoringTypes(IEnumerable<RefactoringType> types)
        {
            SupportedRefactoringsListBox.Items.Clear();
            SupportedRefactoringsListBox.Items.AddRange(types.Select(RefactoringTypeUtil.
                GetRefactoringTypeName).ToArray());
        }




        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

       /// <summary>
       /// Set the current supported refactoring types. These following three methods are the control part 
       /// of the MVC pattnern.
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void ExtractMethod_CheckedChanged(object sender, EventArgs e)
        {
            SetOnlySupportedRefactoringTypeIfRadioButtonChecked(sender, RefactoringType.EXTRACT_METHOD);
        }

        private void InlineMethod_CheckedChanged(object sender, EventArgs e)
        {

            SetOnlySupportedRefactoringTypeIfRadioButtonChecked(sender, RefactoringType.INLINE_METHOD);
        }

        private void ChangeSignature_CheckedChanged(object sender, EventArgs e)
        {
            SetOnlySupportedRefactoringTypeIfRadioButtonChecked(sender, RefactoringType.
                CHANGE_METHOD_SIGNATURE);
        }

        private void onNoneRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb != null && rb.Checked)
            {
                GhostFactorComponents.configurationComponent.RemoveSupportedRefactoringTypes(
                   RefactoringTypeUtil.GetAllValidRefactoringTypes());
            }
        }


        private void SetOnlySupportedRefactoringTypeIfRadioButtonChecked(object sender, RefactoringType type)
        {
            var rb = sender as RadioButton;
            if (rb != null && rb.Checked)
            {
                GhostFactorComponents.configurationComponent.RemoveSupportedRefactoringTypes
                    (RefactoringTypeUtil.GetAllValidRefactoringTypes());
                GhostFactorComponents.configurationComponent.AddSupportedRefactoringTypes
                    (new[] {type});
            }
        }


        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void RefactoringWariningsForm_Load(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

      
    }
}
