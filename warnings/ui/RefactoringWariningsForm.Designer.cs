using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NLog;
using warnings.util;

namespace warnings.ui
{
    partial class RefactoringWariningsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RefactoringWariningsForm));
            this.refactoringWarningsListView = new System.Windows.Forms.ListView();
            this.File = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Line = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Refactoring = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Description = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.removeRefactoringWarningsButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.refactoringCountLabel = new System.Windows.Forms.Label();
            this.ExtractMethod = new System.Windows.Forms.RadioButton();
            this.InlineMethod = new System.Windows.Forms.RadioButton();
            this.ChangeSignature = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.ActiveSourceText = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ActiveSourceFile = new System.Windows.Forms.TextBox();
            this.SupportedRefactoringsListBox = new System.Windows.Forms.ListBox();
            this.Control = new System.Windows.Forms.GroupBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.panel1.SuspendLayout();
            this.Control.SuspendLayout();
            this.SuspendLayout();
            // 
            // refactoringWarningsListView
            // 
            this.refactoringWarningsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.File,
            this.Line,
            this.Refactoring,
            this.Description});
            this.refactoringWarningsListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.refactoringWarningsListView.FullRowSelect = true;
            this.refactoringWarningsListView.Location = new System.Drawing.Point(12, 12);
            this.refactoringWarningsListView.Name = "refactoringWarningsListView";
            this.refactoringWarningsListView.Size = new System.Drawing.Size(510, 284);
            this.refactoringWarningsListView.SmallImageList = this.imageList;
            this.refactoringWarningsListView.TabIndex = 0;
            this.refactoringWarningsListView.UseCompatibleStateImageBehavior = false;
            this.refactoringWarningsListView.View = System.Windows.Forms.View.Details;
            
            this.refactoringWarningsListView.DoubleClick += new System.EventHandler(this.listView1_DoubleClicked);
            // 
            // File
            // 
            this.File.Text = "File";
            this.File.Width = 127;
            // 
            // Line
            // 
            this.Line.Text = "Line";
            this.Line.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Line.Width = 70;
            // 
            // Refactoring
            // 
            this.Refactoring.Text = "Refactoring";
            this.Refactoring.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Refactoring.Width = 117;
            // 
            // Description
            // 
            this.Description.Text = "Description";
            this.Description.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Description.Width = 191;
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "warning-sign.jpg");
            // 
            // removeRefactoringWarningsButton
            // 
            this.removeRefactoringWarningsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.removeRefactoringWarningsButton.Location = new System.Drawing.Point(395, 302);
            this.removeRefactoringWarningsButton.Name = "removeRefactoringWarningsButton";
            this.removeRefactoringWarningsButton.Size = new System.Drawing.Size(117, 32);
            this.removeRefactoringWarningsButton.TabIndex = 1;
            this.removeRefactoringWarningsButton.Text = "Ignore Warnings";
            this.removeRefactoringWarningsButton.UseVisualStyleBackColor = true;
            this.removeRefactoringWarningsButton.Click += new System.EventHandler(this.OnRemoveWarningButtonClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 310);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(199, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Problematic Refactorings Count:";
            // 
            // refactoringCountLabel
            // 
            this.refactoringCountLabel.AutoSize = true;
            this.refactoringCountLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.refactoringCountLabel.Location = new System.Drawing.Point(217, 310);
            this.refactoringCountLabel.Name = "refactoringCountLabel";
            this.refactoringCountLabel.Size = new System.Drawing.Size(15, 16);
            this.refactoringCountLabel.TabIndex = 3;
            this.refactoringCountLabel.Text = "0";
            // 
            // ExtractMethod
            // 
            this.ExtractMethod.AutoSize = true;
            this.ExtractMethod.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ExtractMethod.Location = new System.Drawing.Point(22, 16);
            this.ExtractMethod.Name = "ExtractMethod";
            this.ExtractMethod.Size = new System.Drawing.Size(114, 20);
            this.ExtractMethod.TabIndex = 4;
            this.ExtractMethod.TabStop = true;
            this.ExtractMethod.Text = "Extract Method";
            this.ExtractMethod.UseVisualStyleBackColor = true;
            this.ExtractMethod.CheckedChanged += new System.EventHandler(this.ExtractMethod_CheckedChanged);
            // 
            // InlineMethod
            // 
            this.InlineMethod.AutoSize = true;
            this.InlineMethod.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InlineMethod.Location = new System.Drawing.Point(23, 42);
            this.InlineMethod.Name = "InlineMethod";
            this.InlineMethod.Size = new System.Drawing.Size(105, 20);
            this.InlineMethod.TabIndex = 5;
            this.InlineMethod.TabStop = true;
            this.InlineMethod.Text = "Inline Method";
            this.InlineMethod.UseVisualStyleBackColor = true;
            this.InlineMethod.CheckedChanged += new System.EventHandler(this.InlineMethod_CheckedChanged);
            // 
            // ChangeSignature
            // 
            this.ChangeSignature.AutoSize = true;
            this.ChangeSignature.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangeSignature.Location = new System.Drawing.Point(23, 65);
            this.ChangeSignature.Name = "ChangeSignature";
            this.ChangeSignature.Size = new System.Drawing.Size(133, 20);
            this.ChangeSignature.TabIndex = 6;
            this.ChangeSignature.TabStop = true;
            this.ChangeSignature.Text = "Change Signature";
            this.ChangeSignature.UseVisualStyleBackColor = true;
            this.ChangeSignature.CheckedChanged += new System.EventHandler(this.ChangeSignature_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radioButton1);
            this.panel1.Controls.Add(this.InlineMethod);
            this.panel1.Controls.Add(this.ChangeSignature);
            this.panel1.Controls.Add(this.ExtractMethod);
            this.panel1.Location = new System.Drawing.Point(10, 17);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(170, 120);
            this.panel1.TabIndex = 7;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(14, 154);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(119, 16);
            this.label2.TabIndex = 8;
            this.label2.Text = "Active Source File:";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // ActiveSourceText
            // 
            this.ActiveSourceText.AutoSize = true;
            this.ActiveSourceText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActiveSourceText.Location = new System.Drawing.Point(180, 148);
            this.ActiveSourceText.Name = "ActiveSourceText";
            this.ActiveSourceText.Size = new System.Drawing.Size(0, 16);
            this.ActiveSourceText.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(13, 209);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(153, 16);
            this.label3.TabIndex = 10;
            this.label3.Text = "Supported Refactorings:";
            // 
            // ActiveSourceFile
            // 
            this.ActiveSourceFile.Location = new System.Drawing.Point(16, 174);
            this.ActiveSourceFile.Name = "ActiveSourceFile";
            this.ActiveSourceFile.Size = new System.Drawing.Size(155, 20);
            this.ActiveSourceFile.TabIndex = 11;
            // 
            // SupportedRefactoringsListBox
            // 
            this.SupportedRefactoringsListBox.FormattingEnabled = true;
            this.SupportedRefactoringsListBox.Location = new System.Drawing.Point(16, 229);
            this.SupportedRefactoringsListBox.Name = "SupportedRefactoringsListBox";
            this.SupportedRefactoringsListBox.Size = new System.Drawing.Size(155, 69);
            this.SupportedRefactoringsListBox.TabIndex = 12;
            // 
            // Control
            // 
            this.Control.Controls.Add(this.SupportedRefactoringsListBox);
            this.Control.Controls.Add(this.ActiveSourceFile);
            this.Control.Controls.Add(this.label3);
            this.Control.Controls.Add(this.ActiveSourceText);
            this.Control.Controls.Add(this.label2);
            this.Control.Controls.Add(this.panel1);
            this.Control.Location = new System.Drawing.Point(528, 8);
            this.Control.Name = "Control";
            this.Control.Size = new System.Drawing.Size(190, 310);
            this.Control.TabIndex = 13;
            this.Control.TabStop = false;
            this.Control.Text = "Control";
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton1.Location = new System.Drawing.Point(24, 89);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(59, 20);
            this.radioButton1.TabIndex = 7;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "None";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.onNoneRadioButtonCheckedChanged);
            // 
            // RefactoringWariningsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(722, 341);
            this.Controls.Add(this.Control);
            this.Controls.Add(this.refactoringCountLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.removeRefactoringWarningsButton);
            this.Controls.Add(this.refactoringWarningsListView);
            this.Name = "RefactoringWariningsForm";
            this.Text = "RefactoringWariningForm";
            this.Load += new System.EventHandler(this.RefactoringWariningsForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.Control.ResumeLayout(false);
            this.Control.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView refactoringWarningsListView;
        private System.Windows.Forms.ColumnHeader File;
        private System.Windows.Forms.ColumnHeader Line;
        private System.Windows.Forms.ColumnHeader Refactoring;
        private System.Windows.Forms.ColumnHeader Description;

        private Button removeRefactoringWarningsButton;
        private ImageList imageList;
        private Label label1;
        private Label refactoringCountLabel;
        private RadioButton ExtractMethod;
        private RadioButton InlineMethod;
        private RadioButton ChangeSignature;
        private Panel panel1;
        private Label label2;
        private Label ActiveSourceText;
        private Label label3;
        private TextBox ActiveSourceFile;
        private ListBox SupportedRefactoringsListBox;
        private GroupBox Control;
        private RadioButton radioButton1;
    }



}