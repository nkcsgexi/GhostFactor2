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
            this.removeRefactoringWarningsButton.Location = new System.Drawing.Point(405, 302);
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
            this.refactoringCountLabel.Location = new System.Drawing.Point(211, 310);
            this.refactoringCountLabel.Name = "refactoringCountLabel";
            this.refactoringCountLabel.Size = new System.Drawing.Size(15, 16);
            this.refactoringCountLabel.TabIndex = 3;
            this.refactoringCountLabel.Text = "0";
            // 
            // RefactoringWariningsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(536, 346);
            this.Controls.Add(this.refactoringCountLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.removeRefactoringWarningsButton);
            this.Controls.Add(this.refactoringWarningsListView);
            this.Name = "RefactoringWariningsForm";
            this.Text = "RefactoringWariningForm";
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
    }



}