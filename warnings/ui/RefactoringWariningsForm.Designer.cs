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
            this.panel1.SuspendLayout();
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
            this.refactoringWarningsListView.Location = new System.Drawing.Point(152, 12);
            this.refactoringWarningsListView.Name = "refactoringWarningsListView";
            this.refactoringWarningsListView.Size = new System.Drawing.Size(510, 284);
            this.refactoringWarningsListView.SmallImageList = this.imageList;
            this.refactoringWarningsListView.TabIndex = 0;
            this.refactoringWarningsListView.UseCompatibleStateImageBehavior = false;
            this.refactoringWarningsListView.View = System.Windows.Forms.View.Details;
            this.refactoringWarningsListView.SelectedIndexChanged += new System.EventHandler(this.RefactoringWarninglistViewSelectedIndexChanged);
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
            this.removeRefactoringWarningsButton.Location = new System.Drawing.Point(545, 302);
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
            this.label1.Location = new System.Drawing.Point(152, 312);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(199, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Problematic Refactorings Count:";
            // 
            // refactoringCountLabel
            // 
            this.refactoringCountLabel.AutoSize = true;
            this.refactoringCountLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.refactoringCountLabel.Location = new System.Drawing.Point(357, 312);
            this.refactoringCountLabel.Name = "refactoringCountLabel";
            this.refactoringCountLabel.Size = new System.Drawing.Size(15, 16);
            this.refactoringCountLabel.TabIndex = 3;
            this.refactoringCountLabel.Text = "0";
            // 
            // ExtractMethod
            // 
            this.ExtractMethod.AutoSize = true;
            this.ExtractMethod.Location = new System.Drawing.Point(22, 4);
            this.ExtractMethod.Name = "ExtractMethod";
            this.ExtractMethod.Size = new System.Drawing.Size(97, 17);
            this.ExtractMethod.TabIndex = 4;
            this.ExtractMethod.TabStop = true;
            this.ExtractMethod.Text = "Extract Method";
            this.ExtractMethod.UseVisualStyleBackColor = true;
            this.ExtractMethod.CheckedChanged += new System.EventHandler(this.ExtractMethod_CheckedChanged);
            // 
            // InlineMethod
            // 
            this.InlineMethod.AutoSize = true;
            this.InlineMethod.Location = new System.Drawing.Point(23, 43);
            this.InlineMethod.Name = "InlineMethod";
            this.InlineMethod.Size = new System.Drawing.Size(89, 17);
            this.InlineMethod.TabIndex = 5;
            this.InlineMethod.TabStop = true;
            this.InlineMethod.Text = "Inline Method";
            this.InlineMethod.UseVisualStyleBackColor = true;
            this.InlineMethod.CheckedChanged += new System.EventHandler(this.InlineMethod_CheckedChanged);
            // 
            // ChangeSignature
            // 
            this.ChangeSignature.AutoSize = true;
            this.ChangeSignature.Location = new System.Drawing.Point(23, 81);
            this.ChangeSignature.Name = "ChangeSignature";
            this.ChangeSignature.Size = new System.Drawing.Size(110, 17);
            this.ChangeSignature.TabIndex = 6;
            this.ChangeSignature.TabStop = true;
            this.ChangeSignature.Text = "Change Signature";
            this.ChangeSignature.UseVisualStyleBackColor = true;
            this.ChangeSignature.CheckedChanged += new System.EventHandler(this.ChangeSignature_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.InlineMethod);
            this.panel1.Controls.Add(this.ChangeSignature);
            this.panel1.Controls.Add(this.ExtractMethod);
            this.panel1.Location = new System.Drawing.Point(1, 101);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(145, 100);
            this.panel1.TabIndex = 7;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // RefactoringWariningsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(674, 348);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.refactoringCountLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.removeRefactoringWarningsButton);
            this.Controls.Add(this.refactoringWarningsListView);
            this.Name = "RefactoringWariningsForm";
            this.Text = "RefactoringWariningForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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
    }



}