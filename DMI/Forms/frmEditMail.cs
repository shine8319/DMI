﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars;

namespace DevExpress.MailClient.Win {
    public partial class frmEditMail : RibbonForm {
        bool isMessageModified;
        bool newMessage = true;
        string messageFrom = string.Empty;
        readonly Message sourceMessage;

        public frmEditMail() {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;
        }
        public frmEditMail(Message message, bool newMessage, string caption) {
            InitializeComponent();
            DictionaryHelper.InitDictionary(spellChecker1);
            this.newMessage = newMessage;
            DialogResult = DialogResult.Cancel;
            this.sourceMessage = message;
            edtSubject.Text = message.Subject;
            edtTo.Text = message.From;
            richEditControl.HtmlText = message.Text;
            this.isMessageModified = newMessage;
            if(string.IsNullOrEmpty(message.From)) 
                splitContainerControl1.PanelVisibility = SplitPanelVisibility.Panel1;
            splitContainerControl1.Collapsed = newMessage;
            ucMessageInfo1.Init(message, ribbonControl.Manager);
            TokenEditHelper.InitializeHistory(edtTo);
            this.sendToEditSize = edtTo.Size;
            this.messageFrom = message.From;
            if(!newMessage) {
                splitContainerControl1.Collapsed = LayoutOption.MailCollapsed;
                lblTo.Text = string.Format("{0}:", caption);
                edtTo.Properties.ReadOnly = true;
                edtSubject.Properties.ReadOnly = true;
                richEditControl.ReadOnly = true;
            }
        }
        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
        }
        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            if(!newMessage)
                LayoutOption.MailCollapsed = splitContainerControl1.Collapsed;
        }
        public Message SourceMessage { get { return sourceMessage; } }
        public bool IsMessageModified {
            get { return isMessageModified || richEditControl.Modified; }
            set {
                isMessageModified = value;
                richEditControl.Modified = value;
            }
        }
        #region SaveMessage event
        EventHandler onSaveMessage;
        public event EventHandler SaveMessage { add { onSaveMessage += value; } remove { onSaveMessage -= value; } }
        protected internal virtual void RaiseSaveMessage() {
            if (onSaveMessage != null)
                onSaveMessage(this, EventArgs.Empty);
        }
        #endregion

        void richEditControl_SelectionChanged(object sender, EventArgs e) {
            tableToolsRibbonPageCategory1.Visible = richEditControl.IsSelectionInTable();
            floatingPictureToolsRibbonPageCategory1.Visible = richEditControl.IsFloatingObjectSelected;
        }
        void edtTo_EditValueChanged(object sender, EventArgs e) {
            isMessageModified = true;
        }
        void edtTo_ValidateToken(object sender, TokenEditValidateTokenEventArgs e) {
            if(!string.IsNullOrEmpty(this.messageFrom) && string.Equals(this.messageFrom, e.Description, StringComparison.OrdinalIgnoreCase)) {
                e.IsValid = true;
                return;
            }
            e.IsValid = TextHelper.IsMailAddressValid(e.Description);
        }
        void edtTo_BeforeShowPopupPanel(object sender, TokenEditBeforeShowPopupPanelEventArgs e) {
            Contact contact = e.Value as Contact;
            this.lblMail.Text = (contact != null) ? contact.FullName.ToString() : e.Description;
            this.lblDescription.Text = e.Description;
            this.picContact.Image = (contact != null && contact.HasPhoto) ? contact.Photo : Properties.Resources.Unknown_user;
        }
        void edtTo_CustomDrawGlyph(object sender, TokenEditCustomDrawTokenGlyphEventArgs e) {
            Image image = GetTagGlyphImage(e.Value);
            if(image != null) e.Cache.Paint.DrawImage(e.Graphics, image, e.GlyphBounds, new Rectangle(Point.Empty, image.Size), true);
            e.Handled = true;
        }
        Image GetTagGlyphImage(object tag) {
            Contact contact = tag as Contact;
            if(contact == null) return Properties.Resources.Owner;
            return contact.Icon;
        }
        Size sendToEditSize = Size.Empty;
        void edtTo_SizeChanged(object sender, EventArgs e) {
            Size newSize = ((TokenEdit)sender).Size;
            panelControl.Height += (newSize.Height - sendToEditSize.Height);
            this.sendToEditSize = newSize;
        }
        void edtSubject_EditValueChanged(object sender, EventArgs e) {
            isMessageModified = true;
        }
        void fileSaveItem1_ItemClick(object sender, ItemClickEventArgs e) {
            ApplyChanges();
        }
        //void frmEditMail_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
        //    if (e.KeyCode == Keys.Escape) {
        //        if (QueryClose() != DialogResult.Cancel)
        //            Close();
        //    }
        //}
        void richEditControl_KeyDown(object sender, KeyEventArgs e) {
            if ((e.Modifiers & Keys.Control) != 0 && e.KeyCode == Keys.S) {
                ApplyChanges();
                e.Handled = true;
            }
        }

        void frmEditMail_FormClosing(object sender, FormClosingEventArgs e) {
            DialogResult result = QueryClose();
            e.Cancel = result == DialogResult.Cancel;
        }
        DialogResult QueryClose() {
            if (!IsMessageModified)
                return DialogResult.Yes;

            DialogResult result = XtraMessageBox.Show(this, Properties.Resources.SaveQuestion, Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
            switch (result) {
                case DialogResult.Cancel:
                    return DialogResult.Cancel;
                case DialogResult.No:
                    return DialogResult.No;
                default:
                case DialogResult.Yes:
                    ApplyChanges();
                    return DialogResult.Yes;
            }
        }
        void ApplyChanges() {
            sourceMessage.Date = DateTime.Now;
            sourceMessage.Text = richEditControl.HtmlText;
            sourceMessage.SetPlainText(ObjectHelper.GetPlainText(richEditControl.Text.TrimStart()));
            sourceMessage.Subject = edtSubject.Text;
            sourceMessage.From = edtTo.EditValue.ToString();
            IsMessageModified = false;
            RaiseSaveMessage();
        }
    }
}
